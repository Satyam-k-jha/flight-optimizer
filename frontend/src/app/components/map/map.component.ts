import { Component, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MapService } from '../../services/map.service';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div id="map" class="h-full w-full rounded-lg shadow-inner z-0"></div>
  `,
  styles: [`
    :host { display: block; height: 100%; width: 100%; min-height: 500px; }
  `]
})
export class MapComponent implements AfterViewInit {

  constructor(private mapService: MapService) { }

  ngAfterViewInit(): void {
    this.mapService.initMap('map');
  }
}
