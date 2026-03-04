import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { EnvioPaqueteComponent } from './pages/envio-paquete/envio-paquete.component';
import { CalculadoraTarifasComponent } from './pages/calculadora-tarifas/calculadora-tarifas.component';
import { BuscadorOficinaComponent } from './pages/buscador-oficina/buscador-oficina.component';
import { PanelUsuarioComponent } from './pages/panel-usuario/panel-usuario.component';
import { PagoExitosoComponent } from './pages/pago-exitoso/pago-exitoso.component';
import { PagoCanceladoComponent } from './pages/pago-cancelado/pago-cancelado.component';
import { ParticularesComponent } from './pages/particulares/particulares.component';
import { EmpresasComponent } from './pages/empresas/empresas.component';
import { AyudaComponent } from './pages/ayuda/ayuda.component';
import { PoliticaPrivacidadComponent } from './pages/politica-privacidad/politica-privacidad.component';
import { TerminosUsoComponent } from './pages/terminos-uso/terminos-uso.component';
import { TrackingComponent } from './pages/tracking/tracking.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'nuevo-envio', component: EnvioPaqueteComponent, canActivate: [authGuard] },
  { path: 'calculadora-tarifas', component: CalculadoraTarifasComponent },
  { path: 'buscador-oficinas', component: BuscadorOficinaComponent },
  { path: 'panel', component: PanelUsuarioComponent, canActivate: [authGuard] },
  { path: 'tracking', component: TrackingComponent },
  { path: 'pago-exitoso', component: PagoExitosoComponent, canActivate: [authGuard] },
  { path: 'pago-cancelado', component: PagoCanceladoComponent, canActivate: [authGuard] },
  { path: 'particulares', component: ParticularesComponent },
  { path: 'empresas', component: EmpresasComponent },
  { path: 'ayuda', component: AyudaComponent },
  { path: 'politica-privacidad', component: PoliticaPrivacidadComponent },
  { path: 'terminos-uso', component: TerminosUsoComponent },
  { path: '**', redirectTo: '' }
];
