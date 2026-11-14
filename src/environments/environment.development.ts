/** Development settings, used by `ng serve`. The API runs from CourseAcademy.Backend with `dotnet run`. */
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5162/api/v1',
  /** Sign-in, users, roles and access use the API. Start it first: see CourseAcademy.Backend/README.md. */
  useApi: true,
};
