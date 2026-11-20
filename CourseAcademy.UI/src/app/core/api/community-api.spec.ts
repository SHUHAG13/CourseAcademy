import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { Course } from '../models/course.model';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from '../services/auth.service';
import { CommunityService } from '../services/community.service';
import { CourseService } from '../services/course.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { MeDto } from './auth.models';
import { CommentDto, PostDto } from './community.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const page = <T>(items: T[], total: number, pageNo = 1) => ok({ items, total, page: pageNo, pageSize: 20 });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const me: MeDto = {
  id: 2, name: 'Demo Student', email: 'student@courseacademy.demo', phone: null, roleCode: 'user', roleName: 'Student',
  permissions: ['dashboard.view', 'learn.view', 'checkout.view', 'community.view', 'community.post'], courseGrants: [],
  hasAdminPanel: false, homeUrl: '/dashboard', createdAt: '2026-11-01T00:00:00Z',
};

const course = { id: 5, slug: 'photo' } as unknown as Course;

const post = (id: number, over: Partial<PostDto> = {}): PostDto => ({
  id, courseId: 5, title: `Post ${id}`, body: 'Body', author: { id: 3, name: 'Tanvir Hasan', badge: 'instructor' },
  createdAt: '2026-11-18T09:00:00Z', editedAt: null, isPinned: false, isLocked: false, isHidden: false, commentCount: 2,
  canEdit: false, canDelete: false, canComment: true, ...over,
});

const comment = (id: number, over: Partial<CommentDto> = {}): CommentDto => ({
  id, postId: 7, courseId: 5, parentId: null, body: `Comment ${id}`, author: { id: null, name: 'Former member', badge: 'student' },
  createdAt: '2026-11-18T10:00:00Z', editedAt: null, isHidden: false, canEdit: false, canDelete: false, replies: [], ...over,
});

describe('Community in API mode', () => {
  let http: HttpTestingController;
  let service: CommunityService;

  const settle = () => new Promise((r) => setTimeout(r, 0));

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_MODE, useValue: true },
        { provide: API_BASE_URL, useValue: BASE },
        { provide: PaymentGateway, useExisting: DemoPaymentGateway },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);
    const login = auth.login(me.email, 'Student@123');
    http.expectOne(`${BASE}/auth/login`).flush(ok({ accessToken: 'a', accessTokenExpiresAt: inOneHour(), refreshToken: 'r', me }));
    await login;
    vi.spyOn(TestBed.inject(CourseService), 'byId').mockImplementation((id) => (id === 5 ? course : undefined));
    service = TestBed.inject(CommunityService);
    for (let round = 0; round < 3; round++) {
      await settle();
      for (const r of http.match(() => true)) r.flush(ok([]));
    }
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });

  async function loadCourse(total: number): Promise<void> {
    const loading = service.loadCourse(5);
    http.expectOne(`${BASE}/courses/photo/community`).flush(ok({ canRead: true, canPost: true, canModerate: false, publicPostCount: total }));
    await settle();
    http.expectOne((r) => r.urlWithParams === `${BASE}/courses/photo/posts?page=1&pageSize=20`).flush(page([post(7, { isPinned: true }), post(8)], total));
    await loading;
  }

  it('loads the first page of posts, then more on request', async () => {
    await loadCourse(3);
    expect(service.publicCount(5)).toBe(3);
    expect(service.postsFor(5).map((p) => p.id)).toEqual(['7', '8']);
    expect(service.badgeOf(service.postsFor(5)[0])).toBe('instructor');
    expect(service.commentCount(service.postsFor(5)[0])).toBe(2);
    expect(service.hasMorePosts(5)).toBe(true);

    const more = service.loadMorePosts(5);
    http.expectOne((r) => r.urlWithParams === `${BASE}/courses/photo/posts?page=2&pageSize=20`).flush(page([post(9)], 3, 2));
    await more;
    expect(service.postsFor(5).map((p) => p.id)).toEqual(['7', '8', '9']);
    expect(service.hasMorePosts(5)).toBe(false);
  });

  it('shows comments with their replies and deleted authors as former members', async () => {
    await loadCourse(2);
    const loading = service.loadComments('7');
    http.expectOne(`${BASE}/posts/7/comments`).flush(ok([comment(1, { replies: [comment(2, { parentId: 1 })] })]));
    await loading;

    const [top] = service.commentsFor(service.postsFor(5)[0]);
    expect(service.authorOf(top)).toBe('Former member');
    expect(service.repliesTo(top).map((r) => r.id)).toEqual(['2']);
  });

  it('sends a reply to the server and reloads the thread and the counts', async () => {
    await loadCourse(2);
    const loading = service.loadComments('7');
    http.expectOne(`${BASE}/posts/7/comments`).flush(ok([comment(1)]));
    await loading;

    const adding = service.addComment('7', 'Thanks!', '1');
    const sent = http.expectOne((r) => r.method === 'POST' && r.url === `${BASE}/posts/7/comments`);
    expect(sent.request.body).toEqual({ body: 'Thanks!', parentId: 1 });
    sent.flush(ok(comment(3, { parentId: 1, body: 'Thanks!' })));
    await settle();
    http.expectOne(`${BASE}/courses/photo/community`).flush(ok({ canRead: true, canPost: true, canModerate: false, publicPostCount: 2 }));
    http.expectOne((r) => r.urlWithParams.startsWith(`${BASE}/courses/photo/posts?`)).flush(page([post(7, { commentCount: 2 }), post(8)], 2));
    http.expectOne(`${BASE}/posts/7/comments`).flush(ok([comment(1, { replies: [comment(3, { parentId: 1, body: 'Thanks!' })] })]));
    await adding;

    expect(service.repliesTo(service.commentsFor(service.postsFor(5)[0])[0]).map((r) => r.body)).toEqual(['Thanks!']);
  });
});
