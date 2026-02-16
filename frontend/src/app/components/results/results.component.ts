import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PathResult } from '../../services/flight.service';
import { CurrencyService, CurrencyCode } from '../../services/currency.service';
import { DurationPipe } from '../../pipes/duration.pipe';
import { CurrencyConverterPipe } from '../../pipes/currency-converter.pipe';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [
    CommonModule,
    DurationPipe,
    CurrencyConverterPipe,
    MatTooltipModule
  ],
  template: `
    <div *ngIf="result" class="bg-white p-6 rounded-lg shadow-md mt-4 relative">
      <!-- Header & Currency Selector -->
      <div class="flex justify-between items-center mb-6">
        <h3 class="text-lg font-bold text-gray-800">Flight Itinerary</h3>
        <select 
          (change)="onCurrencyChange($event)" 
          [value]="selectedCurrency()"
          class="border rounded p-1 text-sm bg-gray-50 hover:bg-gray-100 cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-500">
          <option value="USD">USD ($)</option>
          <option value="EUR">EUR (€)</option>
          <option value="INR">INR (₹)</option>
        </select>
      </div>
      
      <!-- Summary Cards -->
      <div class="grid grid-cols-2 gap-4 mb-6">
        
        <div class="col-span-2 bg-blue-50 p-4 rounded-lg flex flex-col items-center justify-center border border-blue-100">
          <span class="text-blue-600 font-semibold text-sm uppercase tracking-wider">Total Price</span>
          <span class="text-3xl font-extrabold text-blue-700 mt-1 whitespace-nowrap">
            {{ result.totalPrice | currencyConverter | currency:selectedCurrency() }}
          </span>
        </div>

        <div class="col-span-1 bg-green-50 p-4 rounded-lg flex flex-col items-center justify-center border border-green-100">
          <span class="text-green-600 font-semibold text-sm uppercase tracking-wider">Duration</span>
          <span class="text-xl font-bold text-green-700 mt-1">
            {{ result.totalDuration | duration }}
          </span>
        </div>

        <div class="col-span-1 bg-purple-50 p-4 rounded-lg flex flex-col items-center justify-center border border-purple-100">
          <span class="text-purple-600 font-semibold text-sm uppercase tracking-wider">Stops</span>
          <span class="text-xl font-bold text-purple-700 mt-1">
            {{ result.totalStops }}
          </span>
        </div>
      </div>
      
      <!-- Segments Visualization -->
      <div class="mt-4 space-y-4">
        <h4 class="font-semibold text-gray-700 border-b pb-2">Looking for Details?</h4>
        
        <div class="flex flex-col space-y-4">
          <div *ngFor="let segment of result.segments" class="flex items-center justify-between bg-gray-50 p-3 rounded-lg border border-gray-100 hover:shadow-sm transition-shadow">
            
            <!-- Source -->
            <div class="text-left w-1/4" [matTooltip]="segment.sourceName + ', ' + segment.sourceCountry">
              <span class="text-xl sm:text-2xl font-mono font-bold text-gray-800 block truncate">{{ segment.sourceCode }}</span>
              <span class="text-xs text-gray-500 truncate block">{{ segment.sourceName }}</span>
            </div>

            <!-- Arrow & Info -->
            <div class="flex-1 flex flex-col items-center justify-center px-2">
              <span class="text-xs font-medium text-gray-500 mb-1">
                {{ segment.durationMinutes | duration }}
              </span>
              <div class="w-full h-px bg-gray-300 relative flex items-center justify-center">
                <span class="absolute text-gray-400 -mt-[1px]">✈️</span>
              </div>
              <span class="text-xs font-bold text-blue-600 mt-1">
                {{ segment.price | currencyConverter | currency:selectedCurrency() }}
              </span>
            </div>

            <!-- Dest -->
            <div class="text-right w-1/4" [matTooltip]="segment.destName + ', ' + segment.destCountry">
              <span class="text-xl sm:text-2xl font-mono font-bold text-gray-800 block truncate">{{ segment.destCode }}</span>
              <span class="text-xs text-gray-500 truncate block">{{ segment.destName }}</span>
            </div>

          </div>
        </div>
      </div>
    </div>
  `
})
export class ResultsComponent {
  @Input() result: PathResult | null = null;

  selectedCurrency = this.currencyService.selectedCurrency;

  constructor(public currencyService: CurrencyService) { }

  onCurrencyChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.currencyService.setCurrency(select.value as CurrencyCode);
  }
}
