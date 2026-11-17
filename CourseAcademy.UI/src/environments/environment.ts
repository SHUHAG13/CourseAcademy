/** Production settings (`npm run build`). Every server feature uses the API. */
export const environment = {
  production: true,
  /**
   * Base URL of the ASP.NET Core API, without a trailing slash. `/api/v1` works when the site and the API
   * share a host (e.g. a reverse proxy sends /api to the API). Otherwise set the API's full address here
   * before building, e.g. `https://api.example.com/api/v1`, and add the site's address to the API's
   * Cors:AllowedOrigins.
   */
  apiBaseUrl: '/api/v1',
  /** Certificates and community still show "Coming soon" until they move to the API. */
  useApi: true,
};
