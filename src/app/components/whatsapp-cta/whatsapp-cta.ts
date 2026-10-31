import { Component } from '@angular/core';
import { CONTACT } from '../../core/data/site.data';

@Component({
  selector: 'app-whatsapp-cta',
  templateUrl: './whatsapp-cta.html',
  styleUrl: './whatsapp-cta.scss',
})
export class WhatsappCta {
  protected readonly whatsappLink = `https://wa.me/${CONTACT.whatsapp}`;
  protected readonly phoneLink = CONTACT.phoneHref;
}
