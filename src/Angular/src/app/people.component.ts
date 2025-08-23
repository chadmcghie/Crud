import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService, PersonResponse, RoleDto } from './api.service';

@Component({
  selector: 'app-people',
  template: `
  <h2>People</h2>
  <form [formGroup]="form" (ngSubmit)="save()">
    <input placeholder="Full name" formControlName="fullName" />
    <input placeholder="Phone" formControlName="phone" />
    <label>Roles</label>
    <div *ngFor="let role of roles">
      <input type="checkbox" [value]="role.id" (change)="toggleRole(role.id, $event.target?.checked)" [checked]="selectedRoleIds.has(role.id)"/> {{role.name}}
    </div>
    <button type="submit">{{ editingId ? 'Update' : 'Create' }}</button>
    <button type="button" (click)="reset()" *ngIf="editingId">Cancel</button>
  </form>

  <ul>
    <li *ngFor="let p of people">
      {{p.fullName}} ({{p.phone || 'n/a'}}) - Roles: {{p.roles.map(r => r.name).join(', ')}}
      <button (click)="edit(p)">Edit</button>
      <button (click)="remove(p)">Delete</button>
    </li>
  </ul>
  `
})
export class PeopleComponent implements OnInit {
  people: PersonResponse[] = [];
  roles: RoleDto[] = [];
  form: FormGroup;
  editingId: string | null = null;
  selectedRoleIds = new Set<string>();

  constructor(private api: ApiService, fb: FormBuilder) {
    this.form = fb.group({
      fullName: ['', Validators.required],
      phone: ['']
    });
  }

  ngOnInit() {
    this.refresh();
  }

  private loadRoles() {
    this.api.listRoles().subscribe(r => this.roles = r);
  }

  private loadPeople() {
    this.api.listPeople().subscribe(p => this.people = p);
  }

  refresh() {
    this.loadRoles();
    this.loadPeople();
    this.reset();
  }

  toggleRole(roleId: string, checked: boolean | undefined) {
    if (!roleId) return;
    if (checked) this.selectedRoleIds.add(roleId); else this.selectedRoleIds.delete(roleId);
  }

  save() {
    const value = this.form.value;
    const payload = {
      fullName: value.fullName,
      phone: value.phone,
      roleIds: Array.from(this.selectedRoleIds)
    };

    if (this.editingId) {
      this.api.updatePerson(this.editingId, payload).subscribe(() => this.refresh());
    } else {
      this.api.createPerson(payload).subscribe(() => this.refresh());
    }
  }

  edit(p: PersonResponse) {
    this.editingId = p.id;
    this.form.patchValue({ fullName: p.fullName, phone: p.phone });
    this.selectedRoleIds = new Set(p.roles.map(r => r.id));
  }

  reset() {
    this.editingId = null;
    this.form.reset();
    this.selectedRoleIds.clear();
  }

  remove(p: PersonResponse) {
    this.api.deletePerson(p.id).subscribe(() => this.refresh());
  }
}
