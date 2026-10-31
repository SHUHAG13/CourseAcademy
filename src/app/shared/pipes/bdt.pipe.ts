import { Pipe, PipeTransform } from '@angular/core';

const formatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });

/** Formats a number as Bangladeshi Taka, e.g. 2499 → "৳2,499". */
@Pipe({ name: 'bdt' })
export class BdtPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value == null ? '' : `৳${formatter.format(value)}`;
  }
}
