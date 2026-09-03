import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  FormField,
  form,
  max,
  min,
  minLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import {
  CreateMedicineFormModel,
  CreateMedicineRequest,
  Medicine,
} from '../../models/medicine.model';
import { MedicineService } from '../../services/medicine.service';

type RowTone = 'critical' | 'expiry' | 'stock' | 'none';

@Component({
  selector: 'app-medicine-dashboard',
  imports: [FormField, CurrencyPipe, DatePipe],
  templateUrl: './medicine-dashboard.html',
  styleUrl: './medicine-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicineDashboardComponent implements OnInit {
  private readonly medicineService = inject(MedicineService);

  readonly medicines = signal<Medicine[]>([]);
  readonly filterText = signal('');
  readonly sellQuantities = signal<Record<string, number>>({});
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly sellError = signal<string | null>(null);
  readonly formSuccess = signal<string | null>(null);
  readonly sellingIds = signal<ReadonlySet<string>>(new Set());
  readonly submitting = signal(false);

  readonly filteredMedicines = computed((): Medicine[] => {
    const query = this.filterText().trim().toLowerCase();
    const list = this.medicines();
    if (!query) {
      return list;
    }

    return list.filter(
      (medicine) =>
        medicine.fullName.toLowerCase().includes(query) ||
        medicine.brand.toLowerCase().includes(query),
    );
  });

  readonly createModel = signal<CreateMedicineFormModel>({
    fullName: '',
    notes: '',
    expiryDate: '',
    quantity: null,
    price: null,
    brand: '',
  });

  readonly createForm = form(this.createModel, (schemaPath) => {
    required(schemaPath.fullName, { message: 'Full name is required.' });
    minLength(schemaPath.fullName, 1, { message: 'Full name is required.' });

    required(schemaPath.brand, { message: 'Brand is required.' });
    minLength(schemaPath.brand, 1, { message: 'Brand is required.' });

    required(schemaPath.expiryDate, { message: 'Expiry date is required.' });
    validate(schemaPath.expiryDate, ({ value }) => {
      const raw = value();
      if (!raw) {
        return undefined;
      }

      if (!/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
        return { kind: 'invalidDate', message: 'Enter a valid date (YYYY-MM-DD).' };
      }

      const parsed = new Date(`${raw}T00:00:00`);
      if (Number.isNaN(parsed.getTime())) {
        return { kind: 'invalidDate', message: 'Enter a valid calendar date.' };
      }

      return undefined;
    });

    required(schemaPath.quantity, { message: 'Quantity is required.' });
    min(schemaPath.quantity, 0, { message: 'Quantity must be zero or greater.' });

    required(schemaPath.price, { message: 'Price is required.' });
    min(schemaPath.price, 0.01, { message: 'Price must be at least 0.01.' });
    max(schemaPath.price, 999999.99, { message: 'Price is too large.' });
    validate(schemaPath.price, ({ value }) => {
      const price = value();
      if (price === null || price === undefined) {
        return undefined;
      }

      if (!Number.isFinite(price)) {
        return { kind: 'invalidPrice', message: 'Enter a valid price.' };
      }

      const scaled = Math.round(price * 100);
      if (Math.abs(price * 100 - scaled) > 1e-8) {
        return { kind: 'decimalPlaces', message: 'Price must have at most 2 decimal places.' };
      }

      return undefined;
    });
  });

  ngOnInit(): void {
    void this.loadMedicines();
  }

  onFilterInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.filterText.set(target.value);
  }

  onSellQuantityInput(medicineId: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const parsed = Number.parseInt(target.value, 10);
    this.sellQuantities.update((current) => ({
      ...current,
      [medicineId]: Number.isNaN(parsed) ? 0 : parsed,
    }));
  }

  getSellQuantity(medicineId: string): number {
    return this.sellQuantities()[medicineId] ?? 1;
  }

  isSelling(medicineId: string): boolean {
    return this.sellingIds().has(medicineId);
  }

  rowClass(medicine: Medicine): string {
    const tone = this.resolveRowTone(medicine);
    switch (tone) {
      case 'critical':
        return 'bg-critical';
      case 'expiry':
        return 'bg-expiry-danger';
      case 'stock':
        return 'bg-stock-warning';
      default:
        return '';
    }
  }

  async onCreateSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.formError.set(null);
    this.formSuccess.set(null);
    this.submitting.set(true);

    try {
      const success = await submit(this.createForm, {
        action: async () => {
          const model = this.createModel();
          const request: CreateMedicineRequest = {
            fullName: model.fullName.trim(),
            notes: model.notes.trim(),
            expiryDate: model.expiryDate,
            quantity: model.quantity ?? 0,
            price: Number((model.price ?? 0).toFixed(2)),
            brand: model.brand.trim(),
          };

          try {
            const created = await firstValueFrom(this.medicineService.createMedicine(request));
            this.medicines.update((list) => [...list, created]);
            this.sellQuantities.update((current) => ({ ...current, [created.id]: 1 }));
            this.createModel.set({
              fullName: '',
              notes: '',
              expiryDate: '',
              quantity: null,
              price: null,
              brand: '',
            });
            this.formSuccess.set(`Added "${created.fullName}".`);
          } catch (error: unknown) {
            this.formError.set(this.extractErrorMessage(error, 'Failed to add medicine.'));
            return { kind: 'serverError', message: 'Failed to add medicine.' };
          }

          return undefined;
        },
      });

      if (!success && !this.formError()) {
        this.formError.set('Please fix the validation errors before submitting.');
      }
    } finally {
      this.submitting.set(false);
    }
  }

  async sellMedicine(medicine: Medicine): Promise<void> {
    this.sellError.set(null);
    const quantity = this.getSellQuantity(medicine.id);

    if (!Number.isInteger(quantity) || quantity < 1) {
      this.sellError.set('Sell quantity must be a whole number of at least 1.');
      return;
    }

    if (quantity > medicine.quantity) {
      this.sellError.set(`Insufficient stock for ${medicine.fullName}. Available: ${medicine.quantity}.`);
      return;
    }

    this.sellingIds.update((current) => new Set(current).add(medicine.id));

    try {
      const response = await firstValueFrom(
        this.medicineService.sellMedicine(medicine.id, { quantity }),
      );

      this.medicines.update((list) =>
        list.map((item) => (item.id === response.medicine.id ? response.medicine : item)),
      );
      this.sellQuantities.update((current) => ({ ...current, [medicine.id]: 1 }));
    } catch (error: unknown) {
      this.sellError.set(this.extractErrorMessage(error, `Failed to sell ${medicine.fullName}.`));
    } finally {
      this.sellingIds.update((current) => {
        const next = new Set(current);
        next.delete(medicine.id);
        return next;
      });
    }
  }

  private async loadMedicines(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);

    try {
      const medicines = await firstValueFrom(this.medicineService.getMedicines());
      this.medicines.set(medicines);
      const quantities: Record<string, number> = {};
      for (const medicine of medicines) {
        quantities[medicine.id] = 1;
      }
      this.sellQuantities.set(quantities);
    } catch (error: unknown) {
      this.loadError.set(this.extractErrorMessage(error, 'Failed to load medicines.'));
    } finally {
      this.loading.set(false);
    }
  }

  private resolveRowTone(medicine: Medicine): RowTone {
    const expiresSoon = this.isExpiringSoon(medicine.expiryDate);
    const lowStock = medicine.quantity < 10;

    if (expiresSoon && lowStock) {
      return 'critical';
    }
    if (expiresSoon) {
      return 'expiry';
    }
    if (lowStock) {
      return 'stock';
    }
    return 'none';
  }

  private isExpiringSoon(expiryDate: string): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const expiry = new Date(`${expiryDate}T00:00:00`);
    if (Number.isNaN(expiry.getTime())) {
      return false;
    }

    const diffMs = expiry.getTime() - today.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
    return diffDays < 30;
  }

  private extractErrorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload = (error as { error: unknown }).error;
      if (typeof payload === 'object' && payload !== null) {
        if ('message' in payload && typeof (payload as { message: unknown }).message === 'string') {
          return (payload as { message: string }).message;
        }
        if ('title' in payload && typeof (payload as { title: unknown }).title === 'string') {
          return (payload as { title: string }).title;
        }
        if ('errors' in payload && typeof (payload as { errors: unknown }).errors === 'object') {
          const errors = (payload as { errors: Record<string, string[]> }).errors;
          const first = Object.values(errors).flat()[0];
          if (first) {
            return first;
          }
        }
      }
      if (typeof payload === 'string' && payload.trim().length > 0) {
        return payload;
      }
    }

    return fallback;
  }
}