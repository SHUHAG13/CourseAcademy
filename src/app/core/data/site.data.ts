// Demo content for the Sports Media, Career and home sections.

export type SportsTopic = 'Cricket' | 'Football' | 'Analysis' | 'Interview' | 'Journalism';

export interface SportsStory {
  id: number;
  topic: SportsTopic;
  title: string;
  excerpt: string;
  date: string;
  readMinutes: number;
  image?: string;
  icon: string;
}

export interface Opportunity {
  id: number;
  role: string;
  team: 'Sports Media' | 'Digital Platform' | 'Software Projects';
  type: 'Part-time' | 'Freelance' | 'Internship';
  skills: string[];
  description: string;
  icon: string;
}

export interface Testimonial {
  name: string;
  course: string;
  outcome: string;
  rating: number;
  text: string;
}

export interface FaqItem {
  question: string;
  answer: string;
}

export const SPORTS_STORIES: SportsStory[] = [
  {
    id: 1,
    topic: 'Cricket',
    title: 'What the top order needs to fix before the home series',
    excerpt: 'Strike rotation in the middle overs has been the quiet problem. We break down the last five innings.',
    date: '2026-10-29',
    readMinutes: 5,
    image: '/images/web/reporter-stadium.webp',
    icon: 'bi-trophy',
  },
  {
    id: 2,
    topic: 'Football',
    title: 'League round-up: late goals and a title race that won’t settle',
    excerpt: 'Three matches decided after the 85th minute. Here is everything you missed this weekend.',
    date: '2026-10-28',
    readMinutes: 4,
    image: '/images/web/pitchside-camera.webp',
    icon: 'bi-dribbble',
  },
  {
    id: 3,
    topic: 'Analysis',
    title: 'Why spin is winning matches on slower pitches',
    excerpt: 'Data from the last season shows a clear shift. Our analysts explain the numbers.',
    date: '2026-10-26',
    readMinutes: 6,
    icon: 'bi-graph-up',
  },
  {
    id: 4,
    topic: 'Interview',
    title: '“Training is where matches are won” — a conversation with a young pacer',
    excerpt: 'On fitness, pressure and what it takes to break into the national side.',
    date: '2026-10-24',
    readMinutes: 7,
    image: '/images/web/sideline-interview.webp',
    icon: 'bi-mic',
  },
  {
    id: 5,
    topic: 'Journalism',
    title: 'Behind the press box: how a match report gets written in 20 minutes',
    excerpt: 'A step-by-step look at deadline reporting, by our students covering live matches.',
    date: '2026-10-21',
    readMinutes: 5,
    icon: 'bi-newspaper',
  },
  {
    id: 6,
    topic: 'Football',
    title: 'Five young Bangladeshi footballers to watch this season',
    excerpt: 'From academy prospects to breakout club stars — the names you’ll be hearing.',
    date: '2026-10-18',
    readMinutes: 4,
    icon: 'bi-star',
  },
];

export const OPPORTUNITIES: Opportunity[] = [
  {
    id: 1,
    role: 'Sports Reporter (Student Desk)',
    team: 'Sports Media',
    type: 'Part-time',
    skills: ['News writing', 'Match reporting'],
    description: 'Cover domestic matches and write news for our sports channel with editor support.',
    icon: 'bi-mic',
  },
  {
    id: 2,
    role: 'Sports Video Editor',
    team: 'Sports Media',
    type: 'Freelance',
    skills: ['Premiere Pro', 'Highlight editing'],
    description: 'Edit match highlights, reels and interview packages for our YouTube and Facebook pages.',
    icon: 'bi-film',
  },
  {
    id: 3,
    role: 'Content Writer',
    team: 'Digital Platform',
    type: 'Freelance',
    skills: ['Content writing', 'SEO'],
    description: 'Write course pages, blog posts and learning guides for the platform.',
    icon: 'bi-pencil-square',
  },
  {
    id: 4,
    role: 'Junior Frontend Developer',
    team: 'Software Projects',
    type: 'Internship',
    skills: ['Angular', 'TypeScript'],
    description: 'Work on this learning platform and client projects alongside senior developers.',
    icon: 'bi-code-slash',
  },
];

export const TESTIMONIALS: Testimonial[] = [
  {
    name: 'Rakibul Islam',
    course: 'Premiere Pro: Professional Video Editing',
    outcome: 'Now freelancing as a video editor',
    rating: 5,
    text: 'The projects were the best part. I finished the course with three edits in my portfolio and got my first paid client a month later.',
  },
  {
    name: 'Sanjida Akter',
    course: 'Sports Journalism Masterclass',
    outcome: 'Writing for a student sports desk',
    rating: 5,
    text: 'Learning from a working journalist made a huge difference. The feedback on my match reports was honest and detailed.',
  },
  {
    name: 'Tanvir Ahmed',
    course: 'Full-Stack Web Development',
    outcome: 'Hired as a junior developer',
    rating: 5,
    text: 'Clear explanations in Bangla, and the capstone project was exactly what I showed in my job interview.',
  },
];

export const FAQS: FaqItem[] = [
  {
    question: 'Are the courses fully online?',
    answer: 'Yes. All lessons are recorded videos you can watch anytime, on any device. Some courses also include live Q&A sessions.',
  },
  {
    question: 'How long will I have access to a course?',
    answer: 'You get lifetime access to the course videos, including future updates to the same course.',
  },
  {
    question: 'How do I pay for a course?',
    answer: 'You can pay with bKash, Nagad or bank transfer. Contact us on WhatsApp, complete the payment and send the transaction ID — access is given after verification.',
  },
  {
    question: 'Will I receive a certificate?',
    answer: 'Yes. You receive a digital certificate after completing all lessons and the final project.',
  },
  {
    question: 'Can I watch on my phone?',
    answer: 'Yes. The courses work on phones, tablets and computers.',
  },
  {
    question: 'Will I get support while learning?',
    answer: 'Yes. Every course includes access to a student support group where instructors answer questions.',
  },
];

export const CONTACT = {
  phone: '+880 1700 000000',
  phoneHref: 'tel:+8801700000000',
  whatsapp: '8801700000000',
  email: 'support@courseacademy.com',
  address: 'Dhaka, Bangladesh',
  social: [
    { label: 'Facebook', icon: 'bi-facebook', href: 'https://facebook.com' },
    { label: 'YouTube', icon: 'bi-youtube', href: 'https://youtube.com' },
    { label: 'Instagram', icon: 'bi-instagram', href: 'https://instagram.com' },
    { label: 'LinkedIn', icon: 'bi-linkedin', href: 'https://linkedin.com' },
  ],
};
