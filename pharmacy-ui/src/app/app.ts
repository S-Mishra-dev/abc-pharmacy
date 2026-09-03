import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MedicineDashboardComponent } from './components/medicine-dashboard/medicine-dashboard';

@Component({
  selector: 'app-root',
  imports: [MedicineDashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
