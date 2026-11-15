import { Permission } from '../models/permission.model';

// TypeScript shapes of the API's identity DTOs. Field names match the API's JSON exactly.

export interface CourseGrantDto {
  courseId: number;
  permissions: Permission[];
}

export interface MeDto {
  id: number;
  name: string;
  email: string;
  phone: string | null;
  roleCode: string;
  roleName: string;
  permissions: Permission[];
  courseGrants: CourseGrantDto[];
  hasAdminPanel: boolean;
  homeUrl: string;
  createdAt: string;
}

export interface TokensDto {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export interface AuthResultDto extends TokensDto {
  me: MeDto;
}

export interface RoleDto {
  code: string;
  name: string;
  description: string;
  isSystem: boolean;
  /** Screen codes, same as the frontend's screen ids. */
  screens: string[];
  permissions: Permission[];
  memberCount: number;
}

export type AccessType = 'full' | 'custom' | 'role';

export interface UserListItemDto {
  id: number;
  name: string;
  email: string;
  phone: string | null;
  roleCode: string;
  roleName: string;
  accessType: AccessType;
  /** The user's own screens when `accessType` is "custom", otherwise null. */
  customScreens: string[] | null;
  courseGrants: CourseGrantDto[];
  createdAt: string;
}

export interface UserAccessDto {
  user: { id: number; name: string; email: string };
  roleCode: string;
  accessType: AccessType;
  screens: string[];
  permissions: Permission[];
  courseGrants: CourseGrantDto[];
  canEdit: boolean;
  lockReason: string | null;
}
