import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Observable, of } from 'rxjs';
import { debounceTime, switchMap, startWith, map, catchError } from 'rxjs/operators';
import { FlightService } from '../../services/flight.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatInputModule,
    MatFormFieldModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css']
})
export class SearchComponent implements OnInit {
  sourceControl = new FormControl<string | any>('', Validators.required);
  destControl = new FormControl<string | any>('', Validators.required);
  criteria = 'Cheapest';

  filteredSourceOptions: Observable<any[]> | undefined;
  filteredDestOptions: Observable<any[]> | undefined;

  @Output() search = new EventEmitter<{ source: string, dest: string, criteria: string }>();

  constructor(
    private flightService: FlightService,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit() {
    this.filteredSourceOptions = this.setupAutocomplete(this.sourceControl);
    this.filteredDestOptions = this.setupAutocomplete(this.destControl);
  }

  private setupAutocomplete(control: FormControl): Observable<any[]> {
    return control.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      switchMap(value => {
        const query = typeof value === 'string' ? value : value?.iataCode;
        if (query && query.length >= 2) {
          return this.flightService.searchAirports(query).pipe(
            catchError(() => of([]))
          );
        } else {
          return of([]);
        }
      })
    );
  }

  displayFn(airport: any): string {
    return airport && airport.iataCode ? `${airport.name} (${airport.iataCode})` : '';
  }

  swapAirports() {
    const sourceVal = this.sourceControl.value;
    const destVal = this.destControl.value;

    this.sourceControl.setValue(destVal);
    this.destControl.setValue(sourceVal);
  }

  onSearch() {
    if (this.sourceControl.invalid || this.destControl.invalid) {
      this.sourceControl.markAsTouched();
      this.destControl.markAsTouched();
      this.snackBar.open('Please select both Source and Destination airports.', 'Close', {
        duration: 3000,
        panelClass: ['bg-red-500', 'text-white']
      });
      return;
    }

    const sourceVal = this.sourceControl.value;
    const destVal = this.destControl.value;

    const sourceIata = typeof sourceVal === 'string' ? sourceVal : sourceVal?.iataCode;
    const destIata = typeof destVal === 'string' ? destVal : destVal?.iataCode;

    if (sourceIata && destIata) {
      this.search.emit({ source: sourceIata, dest: destIata, criteria: this.criteria });
    } else {
      this.snackBar.open('Please select valid airports from the list.', 'Close', { duration: 3000 });
    }
  }
}
