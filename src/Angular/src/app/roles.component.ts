import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ApiService, RoleDto } from './api.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  template: `
  <h2>Roles</h2>
  <form [formGroup]="form" (ngSubmit)="save()">
    <input placeholder="Name" formControlName="name" />
    <input placeholder="Description" formControlName="description" />
    <button type="submit">{{ editingId ? 'Update' : 'Create' }}</button>
    <button type="button" (click)="reset()" *ngIf="editingId">Cancel</button>
  </form>

  <ul>
    <li *ngFor="let r of roles">
      {{r.name}} - {{r.description || 'n/a'}}
      <button (click)="edit(r)">Edit</button>
      <button (click)="remove(r)">Delete</button>
    </li>
  </ul>
  `
})
export class RolesComponent implements OnInit {
  roles: RoleDto[] = [];
  form: FormGroup;
  editingId: string | null = null;

  constructor(private api: ApiService, fb: FormBuilder) {
    this.form = fb.group({
      name: ['', Validators.required],
      description: ['']
    });
  }

  ngOnInit() {
    this.refresh();
  }

  refresh() {
    this.api.listRoles().subscribe(r => this.roles = r);
    this.reset();
  }

  save() {
    const value = this.form.value;
    if (this.editingId) {
      this.api.updateRole(this.editingId, { name: value.name, description: value.description }).subscribe(() => this.refresh());
    } else {
      this.api.createRole({ name: value.name, description: value.description }).subscribe(() => this.refresh());
    }
  }

  edit(r: RoleDto) {
    this.editingId = r.id;
    this.form.patchValue({ name: r.name, description: r.description });
  }

  reset() {
    this.editingId = null;
    this.form.reset();
  }

  remove(r: RoleDto) {
    this.api.deleteRole(r.id).subscribe(() => this.refresh());
  }
}
