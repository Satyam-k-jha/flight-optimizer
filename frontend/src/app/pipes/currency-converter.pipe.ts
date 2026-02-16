import { Pipe, PipeTransform } from '@angular/core';
import { CurrencyService } from '../services/currency.service';

@Pipe({
    name: 'currencyConverter',
    standalone: true,
    pure: false // Impure to react to service state changes if not using signals in pipe input
})
export class CurrencyConverterPipe implements PipeTransform {

    constructor(private currencyService: CurrencyService) { }

    transform(value: number): number {
        return this.currencyService.convert(value);
    }

}
