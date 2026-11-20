import { Comment, CommunityRole, Post } from '../models/community.model';

// TypeScript shapes of the API's community DTOs, and their mapping to the app's models.

export interface CommunityInfoDto {
  canRead: boolean;
  canPost: boolean;
  canModerate: boolean;
  publicPostCount: number;
}

/** id is null and name "Former member" for a deleted account (BR-COM-13). */
export interface AuthorDto {
  id: number | null;
  name: string;
  badge: CommunityRole;
}

export interface PostDto {
  id: number;
  courseId: number;
  title: string;
  body: string;
  author: AuthorDto;
  createdAt: string;
  editedAt: string | null;
  isPinned: boolean;
  isLocked: boolean;
  isHidden: boolean;
  commentCount: number;
  canEdit: boolean;
  canDelete: boolean;
  canComment: boolean;
}

export interface CommentDto {
  id: number;
  postId: number;
  courseId: number;
  parentId: number | null;
  body: string;
  author: AuthorDto;
  createdAt: string;
  editedAt: string | null;
  isHidden: boolean;
  canEdit: boolean;
  canDelete: boolean;
  replies: CommentDto[];
}

export interface ModerationPostDto {
  post: PostDto;
  courseTitle: string;
  courseSlug: string;
}

export interface ModerationCommentDto {
  comment: CommentDto;
  postTitle: string;
  courseTitle: string;
  courseSlug: string;
}

export function fromPostDto(dto: PostDto): Post {
  return {
    id: String(dto.id),
    courseId: dto.courseId,
    authorId: dto.author.id === null ? '' : String(dto.author.id),
    title: dto.title,
    body: dto.body,
    createdAt: dto.createdAt,
    editedAt: dto.editedAt ?? undefined,
    pinned: dto.isPinned,
    locked: dto.isLocked,
    hidden: dto.isHidden,
    author: { name: dto.author.name, badge: dto.author.badge },
    commentCount: dto.commentCount,
    canEdit: dto.canEdit,
    canDelete: dto.canDelete,
    canComment: dto.canComment,
  };
}

/** A comment or reply; its replies are mapped separately. */
export function fromCommentDto(dto: CommentDto): Comment {
  return {
    id: String(dto.id),
    postId: String(dto.postId),
    courseId: dto.courseId,
    parentId: dto.parentId === null ? null : String(dto.parentId),
    authorId: dto.author.id === null ? '' : String(dto.author.id),
    body: dto.body,
    createdAt: dto.createdAt,
    editedAt: dto.editedAt ?? undefined,
    hidden: dto.isHidden,
    author: { name: dto.author.name, badge: dto.author.badge },
    canEdit: dto.canEdit,
    canDelete: dto.canDelete,
  };
}
