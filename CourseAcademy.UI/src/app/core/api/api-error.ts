import { ErrorItem } from './api.models';

/**
 * A failed API call. `message` is English text suitable for a toast (the UI translates it);
 * `errors` holds field-level problems for forms.
 */
export class ApiError extends Error {
  constructor(
    /** HTTP status, or 0 when the server could not be reached. */
    readonly status: number,
    message: string,
    readonly errors: readonly ErrorItem[] = [],
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** The first message for one form field, if any. */
  fieldError(field: string): string | undefined {
    return this.errors.find((e) => e.field === field)?.message;
  }
}

export const NETWORK_ERROR_MESSAGE = 'Cannot reach the server. Check your internet connection and try again.';
export const UNEXPECTED_ERROR_MESSAGE = 'An unexpected error occurred. Please try again later.';
