import { Comment, Post } from '../models/community.model';

// Sample discussions so the course communities have something to show on first run.

const HOUR_MS = 3_600_000;
const hoursAgo = (hours: number) => new Date(Date.now() - hours * HOUR_MS).toISOString();

const SOFTWARE = 4;
const VIDEO = 1;

function post(id: string, courseId: number, authorId: string, hours: number, title: string, body: string, extra: Partial<Post> = {}): Post {
  return { id, courseId, authorId, title, body, createdAt: hoursAgo(hours), pinned: false, locked: false, hidden: false, ...extra };
}

function comment(
  id: string,
  postId: string,
  courseId: number,
  authorId: string,
  hours: number,
  body: string,
  parentId: string | null = null,
  extra: Partial<Comment> = {},
): Comment {
  return { id, postId, courseId, parentId, authorId, body, createdAt: hoursAgo(hours), hidden: false, ...extra };
}

export const DEMO_POSTS: Post[] = [
  post(
    'post-welcome',
    SOFTWARE,
    'user-instructor-sabbir',
    200,
    'Welcome to the course community!',
    'Introduce yourself here: where you are from and what you want to build. Ask questions about any lesson, and help each other out. Please keep it friendly and on topic.',
    { pinned: true },
  ),
  post(
    'post-loops',
    SOFTWARE,
    'user-student',
    30,
    'When should I use a for loop instead of while?',
    'In the "Variables, conditions and loops" lesson both loops print the same numbers. How do I decide which one to use in my own code?',
  ),
  post(
    'post-git',
    SOFTWARE,
    'user-sample',
    12,
    'Git push asks for a password',
    'I followed the Git and GitHub basics lesson, but "git push" asks for my password and then fails. Has anyone else had this?',
  ),
  post(
    'post-spam',
    SOFTWARE,
    'user-sample',
    5,
    'Cheap followers for sale',
    'Message me for cheap social media followers.',
    { hidden: true },
  ),
  post(
    'post-export',
    VIDEO,
    'user-student',
    48,
    'Best export settings for YouTube?',
    'My exported videos look blurry after uploading. Which settings do you recommend?',
  ),
];

export const DEMO_COMMENTS: Comment[] = [
  comment('comment-1', 'post-welcome', SOFTWARE, 'user-student', 190, "Hi everyone! I'm from Dhaka and I want to build my own online shop."),
  comment('comment-2', 'post-welcome', SOFTWARE, 'user-instructor-sabbir', 188, 'Great goal! The capstone project fits that perfectly.', 'comment-1'),
  comment('comment-3', 'post-welcome', SOFTWARE, 'user-sample', 150, 'Hello from Chattogram. Excited to learn Angular.'),
  comment(
    'comment-4',
    'post-loops',
    SOFTWARE,
    'user-instructor-sabbir',
    28,
    'Use a for loop when you know how many times to repeat, like going through an array. Use while when you repeat until something changes, like waiting for valid input.',
  ),
  comment('comment-5', 'post-loops', SOFTWARE, 'user-student', 26, 'That makes sense, thank you!', 'comment-4'),
  comment('comment-6', 'post-loops', SOFTWARE, 'user-sample', 20, 'The worksheet under that video has good practice for this too.'),
  comment(
    'comment-7',
    'post-git',
    SOFTWARE,
    'user-instructor-sabbir',
    10,
    'GitHub no longer accepts account passwords for git. Create a personal access token in GitHub settings and use it as the password, or sign in with GitHub Desktop.',
  ),
  comment('comment-8', 'post-git', SOFTWARE, 'user-sample', 9, 'The token worked. Thanks!', 'comment-7'),
  comment('comment-9', 'post-export', VIDEO, 'user-instructor', 46, 'Export H.264 at your timeline resolution with a high bitrate. The export settings slides list them all.'),
];
