import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from './api-mode';
import { toApiError } from './api.service';
import { AuthSession, SKIP_AUTH } from './auth-session';

/** Sign-in endpoints answer 401 for wrong credentials; that must never trigger a token refresh. */
const NO_REFRESH = /\/auth\/(login|register|refresh|logout)$/;

function withToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

/**
 * Adds the access token to API requests. Renews it first when it is about to expire, and once more
 * when the API answers 401, then repeats the request. Requests to other hosts are left untouched.
 */
export const authInterceptor: HttpInterceptorFn = (req, next: HttpHandlerFn) => {
  const baseUrl = inject(API_BASE_URL);
  const session = inject(AuthSession);
  if (!req.url.startsWith(baseUrl) || req.context.get(SKIP_AUTH)) return next(req);

  const send = () => next(withToken(req, session.accessToken));
  const canRefresh = session.hasSession && !NO_REFRESH.test(req.url);

  const first = canRefresh && session.accessTokenExpired ? from(session.refresh()).pipe(switchMap(() => send())) : send();

  return first.pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !canRefresh || !session.hasSession) {
        return throwError(() => error);
      }
      return from(session.refresh()).pipe(switchMap((ok) => (ok ? send() : throwError(() => error))));
    }),
  );
};

/** Turns every failed API call into an ApiError with a message that is safe to show. */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = inject(API_BASE_URL);
  if (!req.url.startsWith(baseUrl)) return next(req);
  return next(req).pipe(catchError((error: unknown) => throwError(() => toApiError(error))));
};
