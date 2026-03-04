import http from 'k6/http';
import { check, sleep } from 'k6';

/**
 * Test de carga para NexoPostal - Endpoint de tracking
 * Simula tráfico creciente de consultas de seguimiento
 */
export const options = {
  stages: [
    { duration: '1m', target: 50 },   // Ramp up a 50 usuarios
    { duration: '3m', target: 50 },   // Mantener 50 usuarios
    { duration: '1m', target: 100 },  // Ramp up a 100 usuarios
    { duration: '3m', target: 100 },  // Mantener 100 usuarios
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],   // 95% de peticiones < 500ms
    http_req_failed: ['rate<0.05'],     // Menos del 5% de fallos
    http_reqs: ['rate>10'],             // Al menos 10 req/s
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5004';

export default function () {
  // Test 1: Tracking público (GET)
  const trackingRes = http.get(`${BASE_URL}/api/envios/track/NX000000000ES`);
  check(trackingRes, {
    'tracking status is 200 or 404': (r) => r.status === 200 || r.status === 404,
    'tracking response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(0.5);

  // Test 2: Cotización (POST)
  const cotizacionPayload = JSON.stringify({
    peso: 2.5,
    codigoPostalOrigen: '28013',
    codigoPostalDestino: '08001',
    dimensiones: '30x20x15'
  });

  const cotizacionRes = http.post(
    `${BASE_URL}/api/envios/cotizar`,
    cotizacionPayload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(cotizacionRes, {
    'cotización status is 200': (r) => r.status === 200,
    'cotización tiene precio': (r) => JSON.parse(r.body).precio > 0,
    'cotización response time < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(0.5);

  // Test 3: Tarifas (GET)
  const tarifasRes = http.get(`${BASE_URL}/api/tarifas/consultar?peso=3`);
  check(tarifasRes, {
    'tarifas status is 200': (r) => r.status === 200,
    'tarifas response time < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(1);
}

export function handleSummary(data) {
  return {
    'test-results/load-test-summary.json': JSON.stringify(data, null, 2),
  };
}
