import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FlightSegment {
  sourceCode: string;
  sourceName: string;
  sourceCountry: string;
  sourceLatitude: number;
  sourceLongitude: number;
  destCode: string;
  destName: string;
  destCountry: string;
  destLatitude: number;
  destLongitude: number;
  price: number;
  durationMinutes: number;
}

export interface PathResult {
  success: boolean;
  segments: FlightSegment[];
  totalPrice: number;
  totalDuration: number;
  totalStops: number;
  reason?: string;
  message?: string;
}

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FlightService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  searchFlights(source: string, dest: string, criteria: string): Observable<PathResult> {
    const params = new HttpParams()
      .set('source', source)
      .set('destination', dest)
      .set('criteria', criteria);

    return this.http.get<PathResult>(`${this.apiUrl}/flights/search`, { params });
  }

  getRestrictedZones(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/flights/restricted-zones`);
  }

  searchAirports(query: string): Observable<any[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<any[]>(`${this.apiUrl}/airports/search`, { params });
  }
}
