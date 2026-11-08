import { DEMO_CERTIFICATES } from '../data/certificate.data';
import { buildCertificatePdf } from './certificate-pdf';

describe('buildCertificatePdf', () => {
  it('draws a one-page PDF with the name, course, ID and verify link', async () => {
    const certificate = { ...DEMO_CERTIFICATES[0], mode: 'exam' as const, examType: 'project' as const, scorePercent: 85 };
    const doc = await buildCertificatePdf(certificate, 'https://academy.example/verify/CA-2026-K7Q2XM');
    const pdf = doc.output();

    expect(pdf.startsWith('%PDF-')).toBe(true);
    expect(doc.getNumberOfPages()).toBe(1);
    for (const text of ['Certificate of Achievement', 'Sample Learner', 'Sports Journalism & Sports Media', 'CA-2026-K7Q2XM', 'score of 85%']) {
      expect(pdf).toContain(text);
    }
    expect(pdf).toContain('/URI (https://academy.example/verify/CA-2026-K7Q2XM)');
  });
});
