/** One validation or business error from the API. `field` is the camelCase property name, or null. */
export interface ErrorItem {
  field: string | null;
  message: string;
}

/** The envelope every API response uses. */
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: ErrorItem[];
}

/** `data` of a list endpoint. */
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/** Query string values. Empty values are left out; arrays are sent comma separated. */
export type QueryParams = Record<string, string | number | boolean | readonly (string | number)[] | null | undefined>;
