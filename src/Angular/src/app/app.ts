import { Component } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { PeopleComponent } from './people.component';
import { RolesComponent } from './roles.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [HttpClientModule, RolesComponent, PeopleComponent],
  template: `
    <h1>Home</h1>
    <p>API services exposed 1:1 via components.</p>
    <div style="display:flex; gap: 2rem; align-items:flex-start; flex-wrap: wrap;">
      <section style="flex:1; min-width: 320px;">
        <app-roles></app-roles>
      </section>
      <section style="flex:2; min-width: 420px;">
        <app-people></app-people>
      </section>
    </div>
  `
})
export class App {}
