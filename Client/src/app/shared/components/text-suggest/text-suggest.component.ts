import { Component, Input, forwardRef, ElementRef, HostListener, OnInit, OnDestroy, OnChanges, SimpleChanges } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-text-suggest',
  templateUrl: './text-suggest.component.html',
  styleUrls: ['./text-suggest.component.css'],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TextSuggestComponent),
    multi: true
  }]
})
export class TextSuggestComponent implements ControlValueAccessor, OnInit, OnDestroy, OnChanges {
  @Input() placeholder = '';
  @Input() disabled = false;
  @Input() suggestions: string[] = [];

  isOpen = false;
  inputText = '';
  filtered: string[] = [];
  highlightedIndex = -1;

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnInit(): void {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(100),
      distinctUntilChanged()
    ).subscribe(term => this.filter(term));
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['suggestions'] && this.isOpen) {
      this.filter(this.inputText);
    }
  }

  writeValue(value: string): void {
    this.inputText = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.inputText = value;
    this.onChange(value);
    this.searchSubject.next(value);
    if (!this.isOpen) {
      this.open();
    }
  }

  onFocus(): void {
    this.onTouched();
    this.filter(this.inputText);
    this.open();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.close();
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (!this.isOpen) {
      if (event.key === 'ArrowDown') {
        this.open();
        this.filter(this.inputText);
        event.preventDefault();
      }
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        this.highlightedIndex = Math.min(this.highlightedIndex + 1, this.filtered.length - 1);
        event.preventDefault();
        break;
      case 'ArrowUp':
        this.highlightedIndex = Math.max(this.highlightedIndex - 1, -1);
        event.preventDefault();
        break;
      case 'Enter':
        if (this.highlightedIndex >= 0 && this.highlightedIndex < this.filtered.length) {
          this.select(this.filtered[this.highlightedIndex]);
          event.preventDefault();
        }
        break;
      case 'Escape':
        this.close();
        break;
    }
  }

  select(value: string): void {
    this.inputText = value;
    this.onChange(value);
    this.close();
  }

  private open(): void {
    if (this.disabled || this.suggestions.length === 0) return;
    this.isOpen = true;
    this.highlightedIndex = -1;
  }

  private close(): void {
    this.isOpen = false;
    this.highlightedIndex = -1;
  }

  private filter(term: string): void {
    if (!term) {
      this.filtered = this.suggestions.slice(0, 50);
      return;
    }
    const lower = term.toLowerCase();
    this.filtered = this.suggestions
      .filter(s => s.toLowerCase().includes(lower))
      .slice(0, 50);
  }
}
