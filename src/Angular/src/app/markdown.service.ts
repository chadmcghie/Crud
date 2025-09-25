import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { marked } from 'marked';

@Injectable({
  providedIn: 'root'
})
export class MarkdownService {
  private http = inject(HttpClient);

  constructor() {
    // Configure marked options for better rendering
    marked.setOptions({
      gfm: true,
      breaks: false
    });
  }

  getReadmeContent(): Observable<string> {
    return this.http.get('/README.md', { responseType: 'text' })
      .pipe(
        map(markdown => this.parseMarkdown(markdown)),
        catchError(error => {
          console.error('Error loading README.md:', error);
          return of('<p>Unable to load README content.</p>');
        })
      );
  }

  private parseMarkdown(markdown: string): string {
    try {
      let html = marked.parse(markdown) as string;

      // Debug: Log a sample of the HTML to see list structure
      const listSample = html.match(/<ul[\s\S]*?<\/ul>/);
      if (listSample) {
        console.log('Generated list HTML sample:', listSample[0]);
      }

      // Make internal doc links open in new tab and point to GitHub
      html = html.replace(
        /href="(docs\/[^"]+)"/g,
        'href="https://github.com/chadmcghie/Crud/blob/dev/$1" target="_blank"'
      );

      // Make sure external links open in new tab
      html = html.replace(
        /href="(https?:\/\/[^"]+)"/g,
        'href="$1" target="_blank"'
      );

      return html;
    } catch (error) {
      console.error('Error parsing markdown:', error);
      return '<p>Error parsing README content.</p>';
    }
  }
}