# CourseAcademy

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.0.3.

## Frontend-only demo

There is no backend. Courses, accounts, enrollments and payments are stored in this browser's LocalStorage, so they are not secure and are not shared between devices or users. Admins can restore the defaults with **Reset demo data** on the admin overview page.

| Role  | Email                        | Password      |
| ----- | ---------------------------- | ------------- |
| Admin | `admin@courseacademy.demo`   | `Admin@123`   |
| User  | `student@courseacademy.demo` | `Student@123` |

Passwords are stored unprotected and role checks run only in the browser. Real authentication and authorization must be enforced by the server.

### Connecting the ASP.NET Core API

Services read and write through the `Repository` contract in `src/app/core/data/repository.ts`. Provide HTTP implementations for the tokens in `src/app/core/data/repositories.ts`, then replace the `DemoDataService` start-up load if data needs to load differently.

### Connecting a real payment gateway

Checkout talks to the abstract `PaymentGateway` in `src/app/core/payments/payment-gateway.ts`, and `DemoPaymentGateway` is the current implementation. To use SSLCOMMERZ, aamarPay or bKash:

1. Add ASP.NET Core Web API endpoints that start a payment and return the hosted gateway page URL, and that return a payment's verified status.
2. On the server, read the price from the database, hold the secret keys, and verify the gateway's callback (IPN) before granting course access.
3. Add a `PaymentGateway` implementation that calls those endpoints, register it in `src/app/app.config.ts`, and remove the demo payment page (`src/app/pages/checkout/demo-gateway.ts`).

Never put gateway secret keys or payment verification in this frontend: everything shipped to the browser is public, and anything it sends can be altered.

## Languages

The site runs in Bangla by default, with a switch to English in the top bar and the mobile menu. The choice is saved in the browser.

- `LanguageService` in `src/app/core/i18n` holds the current language and formats text, numbers and dates.
- Templates use the `t`, `n` and `ldate` pipes, plus `bdt` for prices: `{{ 'Log in' | t }}`, `{{ '{0} lessons' | t: [count] }}`.
- The English text is the key. To add or change Bangla, edit `bn.translations.ts` (interface) or `bn-content.translations.ts` (default courses and site content).
- Text with no entry is shown as written, so a course an admin types appears in that one language in both modes.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
