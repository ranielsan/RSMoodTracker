import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { AppComponent } from './app.component';

@Component({
  selector: 'app-mood-form',
  template: ''
})
class MoodFormStubComponent { }

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [
        AppComponent,
        MoodFormStubComponent
      ]
    }).compileComponents();
  });

  it('should display the mood form', () => {
    // Arrange
    const fixture = TestBed.createComponent(AppComponent);

    // Act
    fixture.detectChanges();

    // Assert
    expect(
      fixture.debugElement.query(By.directive(MoodFormStubComponent))
    ).not.toBeNull();
  });
});