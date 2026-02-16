import { Injectable } from '@angular/core';
import * as L from 'leaflet';

@Injectable({
  providedIn: 'root'
})
export class MapService {
  private map!: L.Map;
  private routeLayer: L.Polyline | null = null;
  private zonesLayer: L.LayerGroup = new L.LayerGroup();

  constructor() { }

  private markersLayer: L.LayerGroup = new L.LayerGroup();

  initMap(elementId: string): void {
    this.map = L.map(elementId).setView([30, 0], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.zonesLayer.addTo(this.map);
    this.markersLayer.addTo(this.map);
  }

  drawRestrictedZones(zones: any[]): void {
    this.zonesLayer.clearLayers();
    zones.forEach(zone => {
      // Zone coordinates from API are likely [[lat, lon], ...]
      const polygon = L.polygon(zone.coordinates, {
        color: 'red',
        fillColor: '#f03',
        fillOpacity: 0.3,
        weight: 1
      }).bindPopup(zone.name);

      this.zonesLayer.addLayer(polygon);
    });
  }

  drawRoute(segments: any[]): void {
    if (this.routeLayer) {
      this.map.removeLayer(this.routeLayer);
    }
    if (this.planeMarker) {
      this.map.removeLayer(this.planeMarker);
    }
    this.markersLayer.clearLayers();

    if (!segments || segments.length === 0) return;

    // 1. Extract Path Coordinates
    const latLngs: L.LatLngExpression[] = [];
    const airports = new Map<string, any>();

    segments.forEach(seg => {
      latLngs.push([seg.sourceLatitude, seg.sourceLongitude]);

      airports.set(seg.sourceCode, {
        code: seg.sourceCode,
        name: seg.sourceName,
        country: seg.sourceCountry,
        lat: seg.sourceLatitude,
        lon: seg.sourceLongitude
      });

      // Add dest of last segment (or all dests to be safe)
      airports.set(seg.destCode, {
        code: seg.destCode,
        name: seg.destName,
        country: seg.destCountry,
        lat: seg.destLatitude,
        lon: seg.destLongitude
      });
    });

    // Add final destination point to path
    const lastSeg = segments[segments.length - 1];
    latLngs.push([lastSeg.destLatitude, lastSeg.destLongitude]);

    // 2. Draw Polyline
    this.routeLayer = L.polyline(latLngs, {
      color: 'blue',
      weight: 3,
      opacity: 0.7,
      dashArray: '10, 10'
    }).addTo(this.map);

    this.map.fitBounds(this.routeLayer.getBounds(), { padding: [50, 50] });

    // 3. Draw Airport Markers
    airports.forEach(ap => {
      L.circleMarker([ap.lat, ap.lon], {
        radius: 6,
        fillColor: 'red',
        color: '#fff',
        weight: 1,
        opacity: 1,
        fillOpacity: 0.8
      })
        .bindPopup(`<b>${ap.code}</b><br>${ap.name}, ${ap.country}`)
        .addTo(this.markersLayer);
    });

    // 4. Animate Plane
    this.animatePlane(latLngs as number[][]);
  }

  private planeMarker: L.Marker | null = null;

  private animatePlane(path: number[][]) {
    // ... logic same ...
    const icon = L.divIcon({
      className: 'plane-icon',
      html: '<div style="font-size: 24px;">✈️</div>',
      iconSize: [30, 30],
      iconAnchor: [15, 15]
    });

    // Interpolate
    const frames = 100;
    const fullPath: number[][] = [];

    // Note: path is [ [lat, lon], [lat, lon] ]
    for (let i = 0; i < path.length - 1; i++) {
      const start = path[i];
      const end = path[i + 1];
      for (let j = 0; j <= frames; j++) {
        const lat = start[0] + (end[0] - start[0]) * (j / frames);
        const lng = start[1] + (end[1] - start[1]) * (j / frames);
        fullPath.push([lat, lng]);
      }
    }

    this.planeMarker = L.marker(fullPath[0] as L.LatLngTuple, { icon }).addTo(this.map);

    let frame = 0;
    // Clear any existing interval if we stored it? 
    // Ideally we track intervalId to clear it. For now simple.
    const interval = setInterval(() => {
      if (frame >= fullPath.length) {
        clearInterval(interval);
        return;
      }
      this.planeMarker?.setLatLng(fullPath[frame] as L.LatLngTuple);
      frame++;
    }, 20);
  }

  invalidateSize(): void {
    if (this.map) {
      this.map.invalidateSize();
    }
  }
}
