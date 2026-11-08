import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from './auth.service';
import { CommunityService } from './community.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

const SOFTWARE = 4;

describe('CommunityService', () => {
  let auth: AuthService;
  let community: CommunityService;

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PaymentGateway, useExisting: DemoPaymentGateway }],
    });
    auth = TestBed.inject(AuthService);
    community = TestBed.inject(CommunityService);
    await Promise.all([
      auth.ready,
      community.ready,
      TestBed.inject(CourseService).ready,
      TestBed.inject(PermissionService).ready,
      TestBed.inject(EnrollmentService).ready,
    ]);
  });

  async function logIn(email: string, password = 'Student@123'): Promise<void> {
    vi.useFakeTimers({ toFake: ['setTimeout'] });
    const login = auth.login(email, password);
    await vi.advanceTimersByTimeAsync(1000);
    await login;
    vi.useRealTimers();
  }

  it('is closed to visitors and to students of other courses', async () => {
    expect(community.canRead(SOFTWARE)).toBe(false);
    expect(community.postsFor(SOFTWARE)).toEqual([]);
    expect(community.publicCount(SOFTWARE)).toBe(3);

    await logIn('student@courseacademy.demo');
    expect(community.canRead(SOFTWARE)).toBe(true);
    expect(community.canRead(2)).toBe(false);
    await expect(community.createPost(2, 'Hello', 'Anyone here?')).rejects.toBeInstanceOf(PermissionDeniedError);
  });

  it('shows pinned posts first and hides moderated posts from students', async () => {
    await logIn('student@courseacademy.demo');
    const posts = community.postsFor(SOFTWARE);
    expect(posts[0].pinned).toBe(true);
    expect(posts.some((p) => p.hidden)).toBe(false);

    await logIn('admin@courseacademy.demo', 'Admin@123');
    expect(community.postsFor(SOFTWARE).some((p) => p.hidden)).toBe(true);
  });

  it('lets students post, comment and reply, keeping replies one level deep', async () => {
    await logIn('student@courseacademy.demo');
    const post = await community.createPost(SOFTWARE, '  Angular question  ', 'How do signals work?');
    expect(post.title).toBe('Angular question');

    const comment = await community.addComment(post.id, 'Good question!');
    const reply = await community.addComment(post.id, 'Thanks', comment.id);
    const replyToReply = await community.addComment(post.id, 'Me too', reply.id);
    expect(reply.parentId).toBe(comment.id);
    expect(replyToReply.parentId).toBe(comment.id);
    expect(community.repliesTo(comment).length).toBe(2);
  });

  it('lets only authors edit, and authors or moderators delete', async () => {
    await logIn('student@courseacademy.demo');
    await expect(community.editPost('post-git', 'Mine now', 'Edited')).rejects.toBeInstanceOf(PermissionDeniedError);
    await expect(community.deletePost('post-git')).rejects.toBeInstanceOf(PermissionDeniedError);
    await expect(community.editPost('post-loops', 'For or while?', 'Updated question')).resolves.toBeTruthy();

    await logIn('sabbir@courseacademy.demo', 'Instructor@123');
    await community.deletePost('post-git');
    expect(community.posts().some((p) => p.id === 'post-git')).toBe(false);
    expect(community.comments().some((c) => c.postId === 'post-git')).toBe(false);
  });

  it('lets instructors moderate their own course only', async () => {
    await logIn('sabbir@courseacademy.demo', 'Instructor@123');
    expect(community.roleIn('user-instructor-sabbir', SOFTWARE)).toBe('instructor');
    expect(community.canModerate(SOFTWARE)).toBe(true);
    expect(community.canModerate(1)).toBe(false);
    await expect(community.setPostFlags('post-export', { hidden: true })).rejects.toBeInstanceOf(PermissionDeniedError);

    await community.setPostFlags('post-loops', { locked: true });
    await expect(community.addComment('post-loops', 'Moderators can still answer')).resolves.toBeTruthy();

    await logIn('student@courseacademy.demo');
    await expect(community.addComment('post-loops', 'One more thing')).rejects.toThrow('locked');
    await expect(community.setPostFlags('post-loops', { locked: false })).rejects.toBeInstanceOf(PermissionDeniedError);
  });
});
