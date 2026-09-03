export interface Medicine {
  id: string;
  fullName: string;
  notes: string;
  expiryDate: string;
  quantity: number;
  price: number;
  brand: string;
}

export interface SaleRecord {
  id: string;
  medicineId: string;
  medicineName: string;
  quantitySold: number;
  totalPrice: number;
  saleDate: string;
}

export interface CreateMedicineRequest {
  fullName: string;
  notes: string;
  expiryDate: string;
  quantity: number;
  price: number;
  brand: string;
}

export interface SellMedicineRequest {
  quantity: number;
}

export interface SellMedicineResponse {
  medicine: Medicine;
  sale: SaleRecord;
}

export interface CreateMedicineFormModel {
  fullName: string;
  notes: string;
  expiryDate: string;
  quantity: number | null;
  price: number | null;
  brand: string;
}