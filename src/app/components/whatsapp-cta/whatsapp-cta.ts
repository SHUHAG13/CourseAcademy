import { Component } from '@angular/core';

@Component({
  selector: 'app-whatsapp-cta',
  imports: [],
  templateUrl: './whatsapp-cta.html',
  styleUrl: './whatsapp-cta.scss',
})
export class WhatsappCta {
 whatsappNumber = '8801700000000';

  get whatsappLink(): string {
    return `https://wa.me/${this.whatsappNumber}`;
  }

  get phoneLink(): string {
    return `tel:+${this.whatsappNumber}`;
  }
}
