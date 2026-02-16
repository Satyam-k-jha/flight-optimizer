import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightService, PathResult } from './services/flight.service';
import { MapService } from './services/map.service';
import { SearchComponent } from './components/search/search.component';
import { MapComponent } from './components/map/map.component';
import { ResultsComponent } from './components/results/results.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, SearchComponent, MapComponent, ResultsComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  searchResult: PathResult | null = null;
  loading = false;
  error = '';
  errorType: 'warning' | 'critical' = 'warning';

  constructor(
    private flightService: FlightService,
    private mapService: MapService
  ) { }

  ngOnInit() {
    // Load Restricted Zones on startup
    this.flightService.getRestrictedZones().subscribe({
      next: (zones) => {
        // Give map time to init
        setTimeout(() => this.mapService.drawRestrictedZones(zones), 1000);
      },
      error: (err) => console.error('Failed to load zones', err)
    });
  }

  onSearch(criteria: { source: string, dest: string, criteria: string }) {
    this.loading = true;
    this.error = '';
    this.searchResult = null;
    this.mapService.drawRoute([]); // Clear previous

    this.flightService.searchFlights(criteria.source, criteria.dest, criteria.criteria)
      .subscribe({
        next: (res) => {
          this.loading = false;
          if (res.success) {
            this.searchResult = res;
            this.mapService.drawRoute(res.segments);
          } else {
            this.error = res.message || 'Unknown error occurred.';
            // Check reason
            if (res.reason === 'RestrictedZoneBlock' || (res as any).reason === 2) { // 2 = RestrictedZoneBlock
              this.errorType = 'critical';
            } else {
              this.errorType = 'warning'; // NoRoute
            }
          }
        },
        error: (err) => {
          this.loading = false;
          // Fallback for network errors or unhandled codes
          if (err.status === 409) {
            this.error = err.error?.message || 'Flight path blocked by Restricted Zones.';
            this.errorType = 'critical';
          } else if (err.status === 404) {
            this.error = err.error?.message || 'No valid route found.';
            this.errorType = 'warning';
          } else {
            this.error = 'An error occurred while calculating the route. Ensure API is running.';
            this.errorType = 'warning';
          }
          console.error(err);
        }
      });
  }
}
