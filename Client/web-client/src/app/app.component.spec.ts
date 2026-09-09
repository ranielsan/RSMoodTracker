import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, RouterOutlet } from '@angular/router';

import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      imports: [RouterOutlet],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should provide an outlet for routed pages', () => {
    // Arrange
    const fixture = TestBed.createComponent(AppComponent);

    // Act
    fixture.detectChanges();

    // Assert
    expect(
      fixture.debugElement.query(By.directive(RouterOutlet))
    ).not.toBeNull();
  });
});