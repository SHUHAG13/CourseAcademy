import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { API_MODE } from '../../../core/api/api-mode';
import { uid } from '../../../core/data/repository';
import {
  CourseModule,
  DOCUMENT_TYPES,
  DocumentType,
  MAX_SECTIONS_PER_SUBMODULE,
  MAX_TOPIC_DEPTH,
  MIN_SECTIONS_PER_SUBMODULE,
  MediaFile,
  MediaStatus,
  Section,
  SectionDocument,
  SectionVideo,
  Submodule,
} from '../../../core/models/course.model';
import { CourseService, documentUrlMatchesType } from '../../../core/services/course.service';
import { ACCEPT, MediaKind, MediaService } from '../../../core/services/media.service';
import { failureMessage } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { URL_PATTERN } from '../../../shared/forms/validators';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

type Rows = FormArray<FormGroup>;

/** Edits a course's modules, submodules, sections, and each section's videos and documents. */
@Component({
  selector: 'app-curriculum-editor',
  imports: [TranslatePipe, ReactiveFormsModule, RouterLink, EmptyState, NgTemplateOutlet],
  templateUrl: './curriculum-editor.html',
  styleUrl: './curriculum-editor.scss',
})
export class CurriculumEditor implements OnInit {
  private readonly courses = inject(CourseService);
  private readonly media = inject(MediaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly changes = inject(ChangeDetectorRef);
  /** API mode: videos are uploaded (older ones keep their link); documents may be uploaded or linked. */
  protected readonly apiMode = inject(API_MODE);
  private destroyed = false;

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly course = computed(() => this.courses.byId(Number(this.id())));
  protected readonly documentTypes = DOCUMENT_TYPES;
  protected readonly minSections = MIN_SECTIONS_PER_SUBMODULE;
  protected readonly maxSections = MAX_SECTIONS_PER_SUBMODULE;
  protected readonly maxDepth = MAX_TOPIC_DEPTH;
  protected readonly saving = signal(false);
  /** Upload progress (0–100) by row id, while a file is being sent. */
  protected readonly uploading = signal<Record<string, number>>({});
  /** Ids of the modules whose contents are hidden, to keep long curricula manageable. */
  protected readonly collapsed = signal(new Set<string>());

  protected readonly form = this.fb.group({ modules: this.fb.array<FormGroup>([]) });

  get modules(): Rows {
    return this.form.controls.modules;
  }

  constructor() {
    inject(DestroyRef).onDestroy(() => (this.destroyed = true));
  }

  ngOnInit(): void {
    this.load(this.course()?.modules ?? []);
  }

  /** Fills the form, and keeps an eye on uploaded files that are still being processed. */
  private load(modules: CourseModule[]): void {
    this.modules.clear();
    modules.forEach((m) => this.modules.push(this.moduleGroup(m)));
    for (const row of this.fileRows()) {
      const file = row.get('media')?.value as MediaFile | null;
      if (file && isProcessing(file.status)) void this.watch(row, file.id);
    }
  }

  protected submodulesOf(module: AbstractControl): Rows {
    return module.get('submodules') as Rows;
  }

  protected sectionsOf(submodule: AbstractControl): Rows {
    return submodule.get('sections') as Rows;
  }

  protected videosOf(section: AbstractControl): Rows {
    return section.get('videos') as Rows;
  }

  protected documentsOf(section: AbstractControl): Rows {
    return section.get('documents') as Rows;
  }

  protected topicsOf(section: AbstractControl): Rows {
    return section.get('topics') as Rows;
  }

  // ── Form rows ───────────────────────────────────────────────

  private moduleGroup(module?: CourseModule): FormGroup {
    return this.fb.group({
      id: [module?.id ?? uid('module')],
      title: [module?.title ?? '', Validators.required],
      submodules: this.fb.array<FormGroup>(
        module ? module.submodules.map((s) => this.submoduleGroup(s)) : [this.submoduleGroup()],
      ),
    });
  }

  /** A new submodule starts with the minimum number of sections, so it is valid in shape. */
  private submoduleGroup(submodule?: Submodule): FormGroup {
    const sections = submodule?.sections ?? Array.from({ length: MIN_SECTIONS_PER_SUBMODULE }, () => undefined);
    return this.fb.group({
      id: [submodule?.id ?? uid('submodule')],
      title: [submodule?.title ?? '', Validators.required],
      sections: this.fb.array<FormGroup>(
        sections.map((s) => this.sectionGroup(s)),
        sectionCountValidator,
      ),
    });
  }

  private sectionGroup(section?: Section): FormGroup {
    return this.fb.group({
      id: [section?.id ?? uid('section')],
      title: [section?.title ?? '', Validators.required],
      videos: this.fb.array<FormGroup>(section ? section.videos.map((v) => this.videoGroup(v)) : [this.videoGroup()]),
      documents: this.fb.array<FormGroup>((section?.documents ?? []).map((d) => this.documentGroup(d))),
      topics: this.fb.array<FormGroup>((section?.topics ?? []).map((t) => this.sectionGroup(t))),
    });
  }

  /**
   * Demo mode: every video needs a link. API mode: a video needs an uploaded file; an older video may keep its
   * link (read-only) until a file replaces it.
   */
  private videoGroup(video?: SectionVideo): FormGroup {
    return this.fb.group(
      {
        id: [video?.id ?? uid('video')],
        title: [video?.title ?? '', Validators.required],
        url: [video?.url ?? '', this.apiMode ? [] : [Validators.required, Validators.pattern(URL_PATTERN)]],
        minutes: [video?.minutes ?? 10, [Validators.required, Validators.min(1)]],
        preview: [video?.preview ?? false],
        ...this.fileControls(video),
      },
      { validators: this.apiMode ? needsFile : [] },
    );
  }

  private documentGroup(document?: Partial<SectionDocument>): FormGroup {
    return this.fb.group(
      {
        id: [document?.id ?? uid('document')],
        title: [document?.title ?? '', Validators.required],
        type: [document?.type ?? ('PDF' as DocumentType), Validators.required],
        // Optional: a document can be listed before its file is added.
        url: [document?.url ?? '', [Validators.pattern(URL_PATTERN)]],
        videoId: [document?.videoId ?? ''],
        ...this.fileControls(document),
      },
      { validators: urlMatchesType },
    );
  }

  /** The uploaded file of a row: its id, its details, and the id the server has saved (to tidy up replaced uploads). */
  private fileControls(item?: Partial<SectionVideo | SectionDocument>) {
    return {
      mediaId: [item?.mediaId ?? (null as number | null)],
      savedMediaId: [item?.mediaId ?? (null as number | null)],
      media: [item?.media ?? (null as MediaFile | null)],
    };
  }

  protected addModule(): void {
    this.modules.push(this.moduleGroup());
    this.form.markAsDirty();
  }

  protected addSubmodule(module: AbstractControl): void {
    this.submodulesOf(module).push(this.submoduleGroup());
    this.form.markAsDirty();
  }

  protected addSection(submodule: AbstractControl): void {
    const sections = this.sectionsOf(submodule);
    if (sections.length >= MAX_SECTIONS_PER_SUBMODULE) return;
    sections.push(this.sectionGroup());
    this.form.markAsDirty();
  }

  protected addTopic(section: AbstractControl): void {
    this.topicsOf(section).push(this.sectionGroup());
    this.form.markAsDirty();
  }

  protected addVideo(section: AbstractControl): void {
    this.videosOf(section).push(this.videoGroup());
    this.form.markAsDirty();
  }

  /** New documents are attached to the section's first video, as most belong to one. */
  protected addDocument(section: AbstractControl): void {
    const firstVideo = this.videosOf(section).at(0)?.get('id')?.value as string | undefined;
    this.documentsOf(section).push(this.documentGroup({ videoId: firstVideo }));
    this.form.markAsDirty();
  }

  protected removeAt(array: FormArray, index: number): void {
    array.removeAt(index);
    this.form.markAsDirty();
  }

  /** Documents attached to the removed video stay, attached to the whole section instead. */
  protected removeVideo(section: AbstractControl, index: number): void {
    const id = this.videosOf(section).at(index).get('id')?.value;
    for (const d of this.documentsOf(section).controls) {
      if (d.get('videoId')?.value === id) d.get('videoId')?.setValue('');
    }
    this.discardUnsavedFile(this.videosOf(section).at(index));
    this.removeAt(this.videosOf(section), index);
  }

  protected removeDocument(section: AbstractControl, index: number): void {
    this.discardUnsavedFile(this.documentsOf(section).at(index));
    this.removeAt(this.documentsOf(section), index);
  }

  // ── Uploads (API mode) ──────────────────────────────────────

  /** Uploads the chosen file for a video or document row, then follows its processing. */
  protected async uploadFile(row: AbstractControl, kind: MediaKind, event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    const course = this.course();
    if (!file || !course) return;

    const rowId = row.get('id')!.value as string;
    this.uploading.update((u) => ({ ...u, [rowId]: 0 }));
    try {
      const uploaded = await this.media.upload(course.id, kind, file, (percent) => this.uploading.update((u) => ({ ...u, [rowId]: percent })));
      this.discardUnsavedFile(row);
      row.patchValue({ mediaId: uploaded.id, media: uploaded, ...(kind === 'document' ? { url: '' } : {}) });
      row.markAsDirty();
      this.form.markAsDirty();
      void this.watch(row, uploaded.id);
    } catch (error) {
      this.toast.error(failureMessage(error, 'The upload did not finish. Check your connection and try again.'));
    } finally {
      this.uploading.update(({ [rowId]: _, ...rest }) => rest);
    }
  }

  /** Takes the uploaded file off a document row, so it can use a link (or nothing) again. */
  protected clearFile(row: AbstractControl): void {
    this.discardUnsavedFile(row);
    row.patchValue({ mediaId: null, media: null });
    row.markAsDirty();
    this.form.markAsDirty();
  }

  protected acceptFor(kind: MediaKind): string {
    return ACCEPT[kind];
  }

  protected fileStatusLabel(file: MediaFile): string {
    if (file.status === 'ready') return 'Ready';
    if (file.status === 'failed') return 'Processing failed';
    return isProcessing(file.status) ? 'Processing…' : 'Removed';
  }

  /** Updates the row while its file is processed; stops when the row gets another file or the page closes. */
  private async watch(row: AbstractControl, mediaId: number): Promise<void> {
    try {
      await this.media.waitUntilProcessed(
        mediaId,
        (file) => {
          if (row.get('mediaId')?.value !== mediaId) return;
          row.get('media')?.setValue(file);
          // Form values are not signals: ask for the row to be redrawn.
          this.changes.markForCheck();
        },
        () => this.destroyed || row.get('mediaId')?.value !== mediaId,
      );
    } catch {
      // The status shown stays as it was; reopening the page checks again.
    }
  }

  /** An upload the server never saved for this row is deleted when it is replaced or its row removed. */
  private discardUnsavedFile(row: AbstractControl): void {
    const mediaId = row.get('mediaId')?.value as number | null;
    if (mediaId && mediaId !== row.get('savedMediaId')?.value) void this.media.remove(mediaId).catch(() => undefined);
  }

  /** Every video and document row in the form. */
  private fileRows(): AbstractControl[] {
    const rows: AbstractControl[] = [];
    const visit = (sections: Rows) => {
      for (const sec of sections.controls) {
        rows.push(...this.videosOf(sec).controls, ...this.documentsOf(sec).controls);
        visit(this.topicsOf(sec));
      }
    };
    for (const m of this.modules.controls) for (const sub of this.submodulesOf(m).controls) visit(this.sectionsOf(sub));
    return rows;
  }

  /** Moves a row up (-1) or down (+1). Used at every level. */
  protected move(array: FormArray, index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= array.length) return;
    const control = array.at(index);
    array.removeAt(index);
    array.insert(target, control);
    this.form.markAsDirty();
  }

  protected toggleCollapsed(id: string): void {
    this.collapsed.update((set) => {
      const next = new Set(set);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  }

  protected invalid(control: AbstractControl | null): boolean {
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  /** Sections and topics in the form. */
  protected sectionCount(): number {
    const count = (rows: Rows): number => rows.controls.reduce((n, sec) => n + 1 + count(this.topicsOf(sec)), 0);
    return this.modules.controls.reduce(
      (n, m) => n + this.submodulesOf(m).controls.reduce((k, s) => k + count(this.sectionsOf(s)), 0),
      0,
    );
  }

  // ── Save ────────────────────────────────────────────────────

  protected async save(): Promise<void> {
    const course = this.course();
    if (!course) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Show every module, so the highlighted fields can be found.
      this.collapsed.set(new Set());
      this.toast.error('Some fields need attention. Check the highlighted rows.');
      return;
    }
    if (course.published && !this.sectionCount()) {
      this.toast.error('A published course needs at least one section. Unpublish it first to remove them all.');
      return;
    }
    if (Object.keys(this.uploading()).length) {
      this.toast.error('Wait until the uploads have finished, then save.');
      return;
    }

    this.saving.set(true);
    try {
      const saved = await this.courses.saveCurriculum(course, this.toModules());
      // API mode: new rows now have server ids (and saved files); a second save must update them, not add them again.
      if (this.apiMode) this.load(saved.modules);
      this.form.markAsPristine();
      this.toast.success('The curriculum of {0} was saved.', [course.title]);
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    } finally {
      this.saving.set(false);
    }
  }

  /** The form already has the curriculum's shape. Titles are trimmed and an empty video choice means the whole section. */
  private toModules(): CourseModule[] {
    const modules = this.form.getRawValue().modules as CourseModule[];
    return modules.map((m) => ({
      ...m,
      title: m.title.trim(),
      submodules: m.submodules.map((sub) => ({
        ...sub,
        title: sub.title.trim(),
        sections: sub.sections.map(cleanSection),
      })),
    }));
  }
}

/** Trims a section's text, level by level through its topics. An empty video choice means the whole section. */
function cleanSection(sec: Section): Section {
  return {
    ...sec,
    title: sec.title.trim(),
    videos: sec.videos.map((v) => ({ ...v, title: v.title.trim(), url: v.url.trim(), mediaId: v.mediaId ?? undefined })),
    documents: sec.documents.map((d) => ({
      ...d,
      title: d.title.trim(),
      url: d.url.trim(),
      videoId: d.videoId || undefined,
      mediaId: d.mediaId ?? undefined,
    })),
    topics: sec.topics.map(cleanSection),
  };
}

function sectionCountValidator(control: AbstractControl): ValidationErrors | null {
  const length = (control as FormArray).length;
  return length < MIN_SECTIONS_PER_SUBMODULE || length > MAX_SECTIONS_PER_SUBMODULE ? { sectionCount: true } : null;
}

/** A document's link, or its uploaded file's name, must have the extension of its type. */
function urlMatchesType(group: AbstractControl): ValidationErrors | null {
  const { url, type, media } = group.value as { url: string; type: DocumentType; media: MediaFile | null };
  const name = media?.fileName ?? url;
  return name && !documentUrlMatchesType(name, type) ? { extension: true } : null;
}

/** API mode: a video needs an uploaded file, unless it is an older video that keeps its link. */
function needsFile(group: AbstractControl): ValidationErrors | null {
  const { url, mediaId } = group.value as { url: string; mediaId: number | null };
  return mediaId || url ? null : { needsFile: true };
}

function isProcessing(status: MediaStatus): boolean {
  return status === 'pending_upload' || status === 'uploaded' || status === 'queued' || status === 'processing';
}
