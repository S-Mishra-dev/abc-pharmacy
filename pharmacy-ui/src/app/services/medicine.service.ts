import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateMedicineRequest,
  Medicine,
  SellMedicineRequest,
  SellMedicineResponse,
} from '../models/medicine.model';

@Injectable({
  providedIn: 'root',
})
export class MedicineService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/medicines';

  getMedicines(): Observable<Medicine[]> {
    return this.http.get<Medicine[]>(this.baseUrl);
  }

  createMedicine(request: CreateMedicineRequest): Observable<Medicine> {
    return this.http.post<Medicine>(this.baseUrl, request);
  }

  sellMedicine(id: string, request: SellMedicineRequest): Observable<SellMedicineResponse> {
    return this.http.post<SellMedicineResponse>(`${this.baseUrl}/${id}/sell`, request);
  }
}