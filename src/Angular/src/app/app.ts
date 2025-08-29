import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { PeopleComponent } from './people.component';
import { RolesComponent } from './roles.component';
import { PeopleListComponent } from './people-list.component';
import { RolesListComponent } from './roles-list.component';
import { PersonResponse, RoleDto } from './api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, 
    HttpClientModule, 
    RolesComponent, 
    PeopleComponent, 
    PeopleListComponent,
    RolesListComponent
  ],
  template: `
    <div class="app-container">
      <header class="app-header">
        <h1>People & Roles Management System</h1>
        <p class="subtitle">Comprehensive CRUD operations for managing people and their roles</p>
      </header>

      <div class="tab-container">
        <div class="tab-buttons">
          <button 
            class="tab-button" 
            [class.active]="activeTab === 'people'" 
            (click)="setActiveTab('people')"
          >
            👥 People Management
          </button>
          <button 
            class="tab-button" 
            [class.active]="activeTab === 'roles'" 
            (click)="setActiveTab('roles')"
          >
            🎭 Roles Management
          </button>
        </div>

        <!-- People Tab -->
        <div class="tab-content" *ngIf="activeTab === 'people'">
          <div class="management-layout">
            <div class="list-section">
              <app-people-list 
                (editPerson)="onEditPerson($event)"
                (addPerson)="onAddPerson()"
                #peopleList>
              </app-people-list>
            </div>
            
            <div class="form-section" *ngIf="showPeopleForm">
              <app-people
                [editingPerson]="editingPerson"
                (personSaved)="onPersonSaved($event)"
                (cancelled)="onCancelPerson()">
              </app-people>
            </div>
          </div>
        </div>

        <!-- Roles Tab -->
        <div class="tab-content" *ngIf="activeTab === 'roles'">
          <div class="management-layout">
            <div class="list-section">
              <app-roles-list 
                (editRole)="onEditRole($event)"
                (addRole)="onAddRole()"
                #rolesList>
              </app-roles-list>
            </div>
            
            <div class="form-section" *ngIf="showRolesForm">
              <app-roles
                [editingRole]="editingRole"
                (roleSaved)="onRoleSaved($event)"
                (cancelled)="onCancelRole()">
              </app-roles>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
    }

    .app-header {
      text-align: center;
      color: white;
      margin-bottom: 30px;
    }

    .app-header h1 {
      margin: 0 0 10px 0;
      font-size: 2.5rem;
      font-weight: 700;
      text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
    }

    .subtitle {
      margin: 0;
      font-size: 1.1rem;
      opacity: 0.9;
      font-weight: 300;
    }

    .tab-container {
      max-width: 1400px;
      margin: 0 auto;
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 10px 30px rgba(0,0,0,0.3);
    }

    .tab-buttons {
      display: flex;
      background: #f8f9fa;
      border-bottom: 1px solid #e9ecef;
    }

    .tab-button {
      flex: 1;
      padding: 16px 24px;
      border: none;
      background: transparent;
      cursor: pointer;
      font-size: 16px;
      font-weight: 500;
      transition: all 0.3s;
      border-bottom: 3px solid transparent;
    }

    .tab-button:hover {
      background: #e9ecef;
    }

    .tab-button.active {
      background: white;
      color: #007bff;
      border-bottom-color: #007bff;
    }

    .tab-content {
      padding: 30px;
      min-height: 70vh;
    }

    .management-layout {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 30px;
      align-items: start;
    }

    .list-section {
      min-width: 0; /* Allows grid item to shrink */
    }

    .form-section {
      width: 600px;
      position: sticky;
      top: 30px;
    }

    /* Responsive design */
    @media (max-width: 1200px) {
      .management-layout {
        grid-template-columns: 1fr;
        gap: 20px;
      }

      .form-section {
        width: 100%;
        position: static;
        max-width: 600px;
        margin: 0 auto;
      }
    }

    @media (max-width: 768px) {
      .app-container {
        padding: 10px;
      }

      .app-header h1 {
        font-size: 2rem;
      }

      .tab-content {
        padding: 20px 15px;
      }

      .tab-button {
        font-size: 14px;
        padding: 12px 16px;
      }
    }
  `]
})
export class App {
  @ViewChild('peopleList') peopleList!: PeopleListComponent;
  @ViewChild('rolesList') rolesList!: RolesListComponent;
  
  activeTab: 'people' | 'roles' = 'people';
  
  // People state
  showPeopleForm = false;
  editingPerson: PersonResponse | null = null;
  
  // Roles state
  showRolesForm = false;
  editingRole: RoleDto | null = null;

  setActiveTab(tab: 'people' | 'roles') {
    this.activeTab = tab;
    // Reset forms when switching tabs
    this.showPeopleForm = false;
    this.showRolesForm = false;
    this.editingPerson = null;
    this.editingRole = null;
  }

  // People methods
  onAddPerson() {
    this.editingPerson = null;
    this.showPeopleForm = true;
  }

  onEditPerson(person: PersonResponse) {
    this.editingPerson = person;
    this.showPeopleForm = true;
  }

  onPersonSaved(_person: PersonResponse) {
    this.showPeopleForm = false;
    this.editingPerson = null;
    // Refresh the people list
    if (this.peopleList) {
      this.peopleList.refresh();
    }
  }

  onCancelPerson() {
    this.showPeopleForm = false;
    this.editingPerson = null;
  }

  // Roles methods
  onAddRole() {
    this.editingRole = null;
    this.showRolesForm = true;
  }

  onEditRole(role: RoleDto) {
    this.editingRole = role;
    this.showRolesForm = true;
  }

  onRoleSaved(_role: RoleDto) {
    this.showRolesForm = false;
    this.editingRole = null;
    // Refresh the roles list
    if (this.rolesList) {
      this.rolesList.refresh();
    }
  }

  onCancelRole() {
    this.showRolesForm = false;
    this.editingRole = null;
  }
}
