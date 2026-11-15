import type { jsPDF } from 'jspdf';
import { Certificate } from '../models/certificate.model';

/** Draws the certificate and downloads it as `certificate-<ID>.pdf`. */
export async function downloadCertificatePdf(certificate: Certificate, verifyUrl: string): Promise<void> {
  (await buildCertificatePdf(certificate, verifyUrl)).save(`certificate-${certificate.code}.pdf`);
}

/**
 * Draws a certificate as an A4 landscape PDF. jsPDF is loaded only when a
 * certificate is downloaded, so it doesn't slow down other pages. The PDF uses
 * jsPDF's built-in Latin fonts, so text is printed in English; names typed in
 * Bangla script will not render correctly.
 */
export async function buildCertificatePdf(certificate: Certificate, verifyUrl: string): Promise<jsPDF> {
  const { jsPDF } = await import('jspdf');
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
  const width = doc.internal.pageSize.getWidth();
  const height = doc.internal.pageSize.getHeight();
  const center = width / 2;

  const navy: [number, number, number] = [20, 33, 61];
  const orange: [number, number, number] = [249, 115, 22];
  const muted: [number, number, number] = [100, 116, 139];

  // Frame
  doc.setDrawColor(...navy);
  doc.setLineWidth(1.6);
  doc.rect(10, 10, width - 20, height - 20);
  doc.setDrawColor(...orange);
  doc.setLineWidth(0.6);
  doc.rect(14, 14, width - 28, height - 28);

  // Brand
  doc.setFillColor(...navy);
  doc.roundedRect(center - 9, 24, 18, 12, 2, 2, 'F');
  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(13);
  doc.text('CA', center, 32.2, { align: 'center' });
  doc.setTextColor(...navy);
  doc.setFontSize(11);
  doc.text('COURSE ACADEMY', center, 44, { align: 'center', charSpace: 1.5 });

  // Title
  const exam = certificate.mode === 'exam';
  doc.setFont('times', 'bold');
  doc.setFontSize(34);
  doc.text(exam ? 'Certificate of Achievement' : 'Certificate of Completion', center, 66, { align: 'center' });

  doc.setFont('helvetica', 'normal');
  doc.setFontSize(13);
  doc.setTextColor(...muted);
  doc.text('This certifies that', center, 82, { align: 'center' });

  doc.setFont('times', 'bolditalic');
  doc.setFontSize(30);
  doc.setTextColor(...navy);
  doc.text(certificate.userName, center, 98, { align: 'center', maxWidth: width - 60 });
  doc.setDrawColor(...orange);
  doc.setLineWidth(0.5);
  doc.line(center - 70, 102, center + 70, 102);

  doc.setFont('helvetica', 'normal');
  doc.setFontSize(13);
  doc.setTextColor(...muted);
  doc.text('has successfully completed the course', center, 114, { align: 'center' });

  doc.setFont('helvetica', 'bold');
  doc.setFontSize(19);
  doc.setTextColor(...navy);
  doc.text(certificate.courseTitle, center, 126, { align: 'center', maxWidth: width - 60 });

  if (exam) {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(12);
    doc.setTextColor(...muted);
    const work = certificate.examType === 'presentation' ? 'presentation' : 'project';
    const score = certificate.scorePercent !== undefined ? ` with a score of ${certificate.scorePercent}%` : '';
    doc.text(`and passed the central exam, a final ${work},${score}.`, center, 137, { align: 'center' });
  }

  // Signature and date
  const issued = new Date(certificate.issuedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' });
  const left = 70;
  const right = width - 70;
  doc.setDrawColor(...muted);
  doc.setLineWidth(0.3);
  doc.line(left - 35, 162, left + 35, 162);
  doc.line(right - 35, 162, right + 35, 162);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(12);
  doc.setTextColor(...navy);
  doc.text(certificate.instructorName || 'Course Academy', left, 158, { align: 'center' });
  doc.text(issued, right, 158, { align: 'center' });
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(10);
  doc.setTextColor(...muted);
  doc.text('Instructor', left, 168, { align: 'center' });
  doc.text('Date of issue', right, 168, { align: 'center' });

  // Verification
  doc.setFontSize(10);
  doc.text(`Certificate ID: ${certificate.code}`, center, 180, { align: 'center' });
  doc.setTextColor(...navy);
  doc.textWithLink(`Verify at ${verifyUrl}`, center - doc.getTextWidth(`Verify at ${verifyUrl}`) / 2, 186, { url: verifyUrl });

  return doc;
}
