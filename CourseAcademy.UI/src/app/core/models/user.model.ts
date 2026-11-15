import { Permission } from './permission.model';

export interface AppUser {
  id: string;
  name: string;
  email: string;
  phone?: string;
  /**
   * Demo only: kept readable in browser storage. A real backend must store a
   * salted hash on the server and never send it to the client.
   */
  password: string;
  /** Id of the user's `Role`. */
  role: string;
  /** Screen access chosen for this user. When absent, the user has their role's permissions. */
  access?: Permission[];
  /** The instructor profile this account teaches as, if any. */
  instructorId?: string;
  createdAt: string;
}
