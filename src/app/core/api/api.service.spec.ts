import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiError, NETWORK_ERROR_MESSAGE, UNEXPECTED_ERROR_MESSAGE } from './api-error';
import { ApiService, toHttpParams } from './api.service';

describe('ApiService', () => {
  let api: ApiService;
  let http: HttpTestingController;
  const base = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('returns the data of a successful response', async () => {
    const result = api.get<{ code: string }[]>('/lookups', { page: 2 });

    const request = http.expectOne(`${base}/lookups?page=2`);
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, data: [{ code: 'Live' }], message: null, errors: [] });

    expect(await result).toEqual([{ code: 'Live' }]);
  });

  it('sends the body for writes', async () => {
    const result = api.post<number>('items', { name: 'A' });

    const request = http.expectOne(`${base}/items`);
    expect(request.request.body).toEqual({ name: 'A' });
    request.flush({ success: true, data: 7, message: null, errors: [] });

    expect(await result).toBe(7);
  });

  it('rejects with the message and field errors from the envelope', async () => {
    const result = api.post('auth/register', {});

    http.expectOne(`${base}/auth/register`).flush(
      { success: false, data: null, message: 'Please fix the highlighted fields.', errors: [{ field: 'email', message: 'Enter a valid email.' }] },
      { status: 400, statusText: 'Bad Request' },
    );

    const error = await result.catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).message).toBe('Please fix the highlighted fields.');
    expect((error as ApiError).fieldError('email')).toBe('Enter a valid email.');
  });

  it('uses a generic message when the server sent no envelope', async () => {
    const result = api.get('x');

    http.expectOne(`${base}/x`).flush('<html>Bad gateway</html>', { status: 502, statusText: 'Bad Gateway' });

    const error = (await result.catch((e: unknown) => e)) as ApiError;
    expect(error.status).toBe(502);
    expect(error.message).toBe(UNEXPECTED_ERROR_MESSAGE);
  });

  it('reports a network failure', async () => {
    const result = api.get('x');

    http.expectOne(`${base}/x`).error(new ProgressEvent('error'), { status: 0 });

    const error = (await result.catch((e: unknown) => e)) as ApiError;
    expect(error.status).toBe(0);
    expect(error.message).toBe(NETWORK_ERROR_MESSAGE);
  });

  it('rejects a 200 response that reports failure', async () => {
    const result = api.get('x');

    http.expectOne(`${base}/x`).flush({ success: false, data: null, message: 'Not allowed.', errors: [] });

    expect(((await result.catch((e: unknown) => e)) as ApiError).message).toBe('Not allowed.');
  });

  it('builds query params without empty values and with comma-separated arrays', () => {
    const params = toHttpParams({ search: '', page: 1, levels: ['Beginner', 'Advanced'], formats: [], sort: null, featured: false });

    expect(params.toString()).toBe('page=1&levels=Beginner,Advanced&featured=false');
  });
});
