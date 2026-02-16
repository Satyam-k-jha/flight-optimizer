import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'duration',
  standalone: true
})
export class DurationPipe implements PipeTransform {

  transform(minutes: number): string {
    if (!minutes && minutes !== 0) return '';
    
    const h = Math.floor(minutes / 60);
    const m = Math.round(minutes % 60);
    
    if (h > 0) {
      return `${h}h ${m}m`;
    }
    return `${m}m`;
  }

}
