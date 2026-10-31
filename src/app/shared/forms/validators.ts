import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Bangladeshi mobile number, with or without +88 / 88 prefix. */
export const BD_PHONE_PATTERN = /^(?:\+?88)?01[3-9]\d{8}$/;

/** At least 8 characters with one letter and one number. */
export const PASSWORD_PATTERN = /^(?=.*[A-Za-z])(?=.*\d).{8,}$/;

export const URL_PATTERN = /^https?:\/\/[^\s.]+\.[^\s]{2,}$/i;

/** Group-level validator: `field` must equal `matchField`. */
export function matchFields(field: string, matchField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const a = group.get(field)?.value;
    const b = group.get(matchField)?.value;
    return a && b && a !== b ? { mismatch: true } : null;
  };
}

/** True when a control should display its error state. */
export function showError(form: FormGroup, name: string): boolean {
  const control = form.get(name);
  return !!control && control.invalid && (control.touched || control.dirty);
}
