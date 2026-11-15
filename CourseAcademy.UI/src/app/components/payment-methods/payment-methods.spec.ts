import { ComponentFixture, TestBed } from '@angular/core/testing';

import { provideRouter } from '@angular/router';
import { PaymentMethods } from './payment-methods';

describe('PaymentMethods', () => {
  let component: PaymentMethods;
  let fixture: ComponentFixture<PaymentMethods>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentMethods],
      providers: [provideRouter([])],
    })
    .compileComponents();

    fixture = TestBed.createComponent(PaymentMethods);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
