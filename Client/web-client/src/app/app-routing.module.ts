import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MoodFormComponent } from './components/mood-form/mood-form.component';
import { AdminLoginComponent } from './components/admin-login/admin-login.component';
import { adminGuard } from './guards/admin.guard';
import { AdminMoodsComponent } from './components/admin-moods/admin-moods.component';

const routes: Routes = [
  {
    path: '',
    component: MoodFormComponent,
    pathMatch: 'full'
  },
  {
    path: 'admin/login',
    component: AdminLoginComponent
  },
  {
    path: 'admin/moods',
    component: AdminMoodsComponent,
    canActivate: [adminGuard]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
