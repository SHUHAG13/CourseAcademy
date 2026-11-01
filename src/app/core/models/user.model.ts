export type UserRole = 'admin' | 'user';

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
  role: UserRole;
  createdAt: string;
}
