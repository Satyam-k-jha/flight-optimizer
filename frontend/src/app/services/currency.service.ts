import { Injectable, signal } from '@angular/core';

export type CurrencyCode = 'USD' | 'EUR' | 'INR';

@Injectable({
    providedIn: 'root'
})
export class CurrencyService {

    // Using Signals for reactive state
    selectedCurrency = signal<CurrencyCode>('USD');

    private rates: Record<CurrencyCode, number> = {
        'USD': 1,
        'EUR': 0.92,
        'INR': 84.0
    };

    constructor() { }

    setCurrency(code: CurrencyCode) {
        this.selectedCurrency.set(code);
    }

    convert(amountInUsd: number): number {
        const rate = this.rates[this.selectedCurrency()];
        return amountInUsd * rate;
    }

    getSymbol(): string {
        switch (this.selectedCurrency()) {
            case 'USD': return '$';
            case 'EUR': return '€';
            case 'INR': return '₹';
            default: return '$';
        }
    }
}
