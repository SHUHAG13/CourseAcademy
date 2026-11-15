/** Production settings. The API is expected on the same host as the site, under /api/v1. */
export const environment = {
  production: true,
  /** Base URL of the ASP.NET Core API, without a trailing slash. */
  apiBaseUrl: '/api/v1',
  /**
   * When false, every feature uses the browser-storage demo data. Features move to the API one by one
   * as the backend is built; until then this stays false.
   */
  useApi: false,
};
