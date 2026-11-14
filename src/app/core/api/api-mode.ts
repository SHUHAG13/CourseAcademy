import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * True when sign-in, users, roles and access come from the ASP.NET Core API instead of browser storage.
 * Features move to the API step by step; those not moved yet keep using the demo data.
 * A token (not a direct import) so tests can run both modes.
 */
export const API_MODE = new InjectionToken<boolean>('API_MODE', {
  providedIn: 'root',
  factory: () => environment.useApi,
});

/** The API base URL, from the environment. A token so tests can point it elsewhere. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
