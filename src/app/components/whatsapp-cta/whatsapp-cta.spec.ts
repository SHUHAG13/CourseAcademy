import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WhatsappCta } from './whatsapp-cta';

describe('WhatsappCta', () => {
  let component: WhatsappCta;
  let fixture: ComponentFixture<WhatsappCta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WhatsappCta]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WhatsappCta);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
