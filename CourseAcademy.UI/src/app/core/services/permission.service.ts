import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiError } from '../api/api-error';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { RoleDto } from '../api/auth.models';
import { COURSE_GRANT_REPOSITORY, ROLE_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { SCREENS, Screen, expandScreens } from '../data/screens.data';
import {
  ADMIN_ROLE_ID,
  ALL_PERMISSIONS,
  COURSE_PERMISSIONS,
  CourseGrant,
  Permission,
  Role,
} from '../models/permission.model';
import { AppUser } from '../models/user.model';
import { AuthService, toGrant } from './auth.service';

/** Thrown by services when the signed-in user may not make a change. The message is English text for a toast. */
export class PermissionDeniedError extends Error {
  constructor(message = 'You do not have permission to do this.') {
    super(message);
    this.name = 'PermissionDeniedError';
  }
}

/** The toast text for a failed change: the permission or API message if that was the cause, otherwise `fallback`. */
export function failureMessage(error: unknown, fallback: string): string {
  return error instanceof PermissionDeniedError || error instanceof ApiError ? error.message : fallback;
}

/** Permissions that can be ticked in the access matrix. `admin.access` is worked out from them instead. */
const ASSIGNABLE: Permission[] = ALL_PERMISSIONS.filter((p) => p !== 'admin.access');

const ADMIN_SCREENS = SCREENS.filter((s) => s.area === 'admin');

/**
 * Role- and screen-based access control. A user has their own screen access if
 * an admin set it, otherwise their role's permissions, plus any course grants.
 * Services call `assert()` before every change, so permissions hold even if a
 * page forgets to hide a button.
 * - API mode: the signed-in user's permissions come from the API, which checks every change again.
 * - Demo mode: everything lives in browser storage.
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);
  private readonly roleStore = new EntityStore(inject(ROLE_REPOSITORY));
  private readonly grantStore = new EntityStore(inject(COURSE_GRANT_REPOSITORY));
  private readonly apiRoles = signal<Role[]>([]);

  readonly ready: Promise<unknown> = this.apiMode ? this.auth.ready : Promise.all([this.roleStore.ready, this.grantStore.ready]);
  readonly recovered = computed(() => !this.apiMode && (this.roleStore.recovered() || this.grantStore.recovered()));
  readonly roles = computed(() => (this.apiMode ? this.apiRoles() : this.roleStore.items()));
  /** Course grants of every user this page knows about. */
  readonly grants = computed(() => (this.apiMode ? this.auth.listedGrants() : this.grantStore.items()));

  private readonly myGrants = computed(() => {
    if (this.apiMode) {
      const me = this.auth.currentSession();
      return me ? me.courseGrants.map((g) => toGrant(String(me.id), g.courseId, g.permissions)) : [];
    }
    return this.grants().filter((g) => g.userId === this.auth.user()?.id);
  });
  private readonly granted = computed(() => {
    if (this.apiMode) {
      const me = this.auth.currentSession();
      const set = new Set<Permission>(me?.permissions ?? []);
      if (me?.hasAdminPanel) set.add('admin.access');
      return set;
    }
    const set = new Set(this.permissionsOf(this.auth.user()));
    if (ADMIN_SCREENS.some((s) => set.has(s.view)) || this.myGrants().length) set.add('admin.access');
    return set;
  });

  constructor() {
    if (this.apiMode) {
      // Roles are needed by the admin screens of whoever signs in next.
      effect(() => {
        const id = this.auth.currentSession()?.id;
        untracked(() => {
          if (id === undefined) this.apiRoles.set([]);
          else void this.loadRoles().catch(() => undefined);
        });
      });
    }
  }

  /** Where this user's account pages live: the first screen they can open. */
  readonly homeUrl = computed(() => {
    const first = ADMIN_SCREENS.find((s) => this.canView(s));
    if (first) return first.section ? `/admin/${first.section}` : first.path;
    return this.can('dashboard.view') ? '/dashboard' : '/';
  });
  readonly hasAdminPanel = computed(() => this.can('admin.access'));

  role(id: string): Role | undefined {
    return this.roles().find((r) => r.id === id);
  }

  /** The permissions a user has on every course: their own screen access, or else their role's. */
  permissionsOf(user: AppUser | null): Permission[] {
    if (!user) return [];
    if (user.role === ADMIN_ROLE_ID) return ALL_PERMISSIONS;
    return user.access ?? this.role(user.role)?.permissions ?? [];
  }

  /** Whether the signed-in user may open a screen. */
  canView(screen: Screen): boolean {
    return screen.courseScoped ? this.canOnAnyCourse(screen.view) : this.can(screen.view);
  }

  /** Whether the signed-in user has `permission`, through their role or a grant on `courseId`. */
  can(permission: Permission, courseId?: number): boolean {
    if (this.granted().has(permission)) return true;
    return courseId !== undefined && this.myGrants().some((g) => g.courseId === courseId && g.permissions.includes(permission));
  }

  /** Whether the signed-in user has `permission` through their role or on at least one course. */
  canOnAnyCourse(permission: Permission): boolean {
    return this.granted().has(permission) || this.myGrants().some((g) => g.permissions.includes(permission));
  }

  assert(permission: Permission, courseId?: number): void {
    if (!this.can(permission, courseId)) throw new PermissionDeniedError();
  }

  grantsFor(userId: string): CourseGrant[] {
    return this.grants().filter((g) => g.userId === userId);
  }

  // ── Admin ───────────────────────────────────────────────────

  async saveRole(role: Role): Promise<Role> {
    this.assert('roles.edit');
    const name = role.name.trim();
    if (!name) throw new Error('Enter a role name.');
    if (this.roles().some((r) => r.id !== role.id && r.name.toLowerCase() === name.toLowerCase())) {
      throw new Error('A role with this name already exists.');
    }
    const existing = this.role(role.id);
    if (this.apiMode) {
      const body = { name, description: role.description.trim(), screens: screenCodes(role.permissions) };
      const saved = existing
        ? await this.api.put<RoleDto>(`roles/${encodeURIComponent(role.id)}`, body)
        : await this.api.post<RoleDto>('roles', body);
      await this.loadRoles();
      return fromRoleDto(saved);
    }
    const permissions =
      role.id === ADMIN_ROLE_ID ? ALL_PERMISSIONS : expandScreens(role.permissions).filter((p) => ASSIGNABLE.includes(p));
    return this.roleStore.save({ ...role, name, description: role.description.trim(), permissions, system: existing?.system ?? false });
  }

  newRoleId(): string {
    return uid('role');
  }

  async removeRole(id: string): Promise<void> {
    this.assert('roles.edit');
    if (this.role(id)?.system) throw new Error('Built-in roles cannot be deleted.');
    if (this.auth.users().some((u) => u.role === id)) {
      throw new Error('Move the users with this role to another role first.');
    }
    if (this.apiMode) {
      await this.api.delete(`roles/${encodeURIComponent(id)}`);
      await this.loadRoles();
      return;
    }
    await this.roleStore.remove(id);
  }

  async setUserRole(userId: string, roleId: string): Promise<void> {
    this.assert('roles.edit');
    const user = this.auth.users().find((u) => u.id === userId);
    if (!user) throw new Error('User not found.');
    if (!this.role(roleId)) throw new Error('Role not found.');
    if (userId === this.auth.user()?.id) throw new Error("You can't change your own role.");
    if (this.apiMode) {
      await this.api.put(`users/${userId}/role`, { roleCode: roleId });
      await this.reloadAdminData();
      return;
    }
    await this.auth.updateUser({ ...user, role: roleId });
  }

  /** Gives a user their own screen access, which replaces their role's permissions. */
  async setUserAccess(userId: string, permissions: Permission[]): Promise<void> {
    const user = this.editableUser(userId);
    const screens = expandScreens(permissions);
    if (this.apiMode) {
      await this.api.put(`users/${userId}/access`, { screens: screenCodes(screens) });
      await this.reloadAdminData();
      return;
    }
    await this.auth.updateUser({ ...user, access: ASSIGNABLE.filter((p) => screens.includes(p)) });
  }

  /** Returns a user to their role's permissions. */
  async resetUserAccess(userId: string): Promise<void> {
    const user = this.editableUser(userId);
    if (this.apiMode) {
      await this.api.delete(`users/${userId}/access`);
      await this.reloadAdminData();
      return;
    }
    await this.auth.updateUser({ ...user, access: undefined });
  }

  /** Sets a user's permissions on one course. An empty list removes the grant. */
  async setCourseGrant(userId: string, courseId: number, permissions: Permission[]): Promise<void> {
    this.assert('access.edit');
    const existing = this.grants().find((g) => g.userId === userId && g.courseId === courseId);
    const allowed = permissions.filter((p) => COURSE_PERMISSIONS.includes(p));
    if (this.apiMode) {
      await this.api.put(`users/${userId}/course-grants/${courseId}`, { permissions: allowed });
      await this.reloadAdminData();
      return;
    }
    if (!allowed.length) {
      if (existing) await this.grantStore.remove(existing.id);
      return;
    }
    await this.grantStore.save({ id: existing?.id ?? uid('grant'), userId, courseId, permissions: allowed });
  }

  /** Clean-up when a course is deleted. In API mode the server removes the grants with the course. */
  async removeGrantsForCourse(courseId: number): Promise<void> {
    this.assert('courses.delete', courseId);
    if (this.apiMode) return;
    for (const g of this.grants().filter((x) => x.courseId === courseId)) {
      await this.grantStore.remove(g.id);
    }
  }

  async reset(): Promise<void> {
    // Roles and grants in API mode live on the server; demo reset does not touch them.
    if (this.apiMode) return;
    await Promise.all([this.roleStore.reset(), this.grantStore.reset()]);
  }

  /** API mode: loads the roles when the signed-in user may see them. */
  async loadRoles(): Promise<void> {
    const me = this.auth.currentSession();
    const allowed: Permission[] = ['roles.view', 'access.view', 'users.view'];
    if (!this.apiMode || !me || !allowed.some((p) => me.permissions.includes(p))) {
      this.apiRoles.set([]);
      return;
    }
    this.apiRoles.set((await this.api.get<RoleDto[]>('roles')).map(fromRoleDto));
  }

  private async reloadAdminData(): Promise<void> {
    await Promise.all([this.auth.loadUsers(), this.loadRoles()]);
  }

  /** Admins always keep full access and nobody edits their own, so nobody can lock everyone out. */
  private editableUser(userId: string): AppUser {
    this.assert('access.edit');
    const user = this.auth.users().find((u) => u.id === userId);
    if (!user) throw new Error('User not found.');
    if (userId === this.auth.user()?.id) throw new Error("You can't change your own access.");
    if (user.role === ADMIN_ROLE_ID) throw new Error('Admin users always have full access.');
    return user;
  }
}

/** Codes of the screens whose View permission is in the list; the API stores access per screen. */
function screenCodes(permissions: readonly Permission[]): string[] {
  return SCREENS.filter((s) => permissions.includes(s.view)).map((s) => s.id);
}

function fromRoleDto(r: RoleDto): Role {
  return { id: r.code, name: r.name, description: r.description, permissions: r.permissions, system: r.isSystem };
}
