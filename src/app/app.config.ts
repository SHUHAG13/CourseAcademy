import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { routes } from './app.routes';
import { LocalizedTitleStrategy } from './core/i18n/localized-title.strategy';
import { DemoPaymentGateway } from './core/payments/demo-payment-gateway';
import { PaymentGateway } from './core/payments/payment-gateway';
import { DemoDataService } from './core/services/demo-data.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppInitializer(() => inject(DemoDataService).load()),
    // Replace with a gateway that calls the ASP.NET Core API to take real payments.
    { provide: PaymentGateway, useExisting: DemoPaymentGateway },
    { provide: TitleStrategy, useClass: LocalizedTitleStrategy },
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
  ],
};
