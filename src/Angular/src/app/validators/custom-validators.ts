import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class CustomValidators {
  static fullName(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;
      
      const namePattern = /^[a-zA-Z\s\-'.]+$/;
      if (!namePattern.test(value)) {
        return { invalidFullName: 'Full name contains invalid characters' };
      }
      
      if (value.length > 200) {
        return { maxLength: 'Full name cannot exceed 200 characters' };
      }
      
      return null;
    };
  }

  static phoneNumber(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;
      
      const phonePattern = /^\+?[\d\s\-().]{7,15}$/;
      if (!phonePattern.test(value)) {
        return { invalidPhone: 'Phone number must be a valid format' };
      }
      
      return null;
    };
  }

  static roleName(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;
      
      const rolePattern = /^[a-zA-Z0-9\s\-_]+$/;
      if (!rolePattern.test(value)) {
        return { invalidRoleName: 'Role name contains invalid characters' };
      }
      
      if (value.length > 100) {
        return { maxLength: 'Role name cannot exceed 100 characters' };
      }
      
      return null;
    };
  }

  static positiveNumber(max?: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value === null || value === undefined || value === '') return null;
      
      const num = Number(value);
      if (isNaN(num)) {
        return { invalidNumber: 'Must be a valid number' };
      }
      
      if (num <= 0) {
        return { minValue: 'Value must be greater than 0' };
      }
      
      if (max && num > max) {
        return { maxValue: `Value cannot exceed ${max}` };
      }
      
      return null;
    };
  }

  static guidArray(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value || !Array.isArray(value)) return null;
      
      const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      const invalidGuids = value.filter((id: unknown) => 
        typeof id !== 'string' || !guidPattern.test(id)
      );
      
      if (invalidGuids.length > 0) {
        return { invalidGuids: 'All IDs must be valid GUIDs' };
      }
      
      return null;
    };
  }
}