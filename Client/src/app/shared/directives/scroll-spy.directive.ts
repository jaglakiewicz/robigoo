import { Directive, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';

@Directive({ selector: '[appScrollSpy]' })
export class ScrollSpyDirective implements OnInit, OnDestroy {
  @Input() sectionSelector = '.form-section';
  @Output() activeSection = new EventEmitter<string>();

  private observer?: IntersectionObserver;

  constructor(private host: ElementRef<HTMLElement>) {}

  ngOnInit(): void {
    this.observer = new IntersectionObserver(
      entries => {
        const visible = entries
          .filter(e => e.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible.length) this.activeSection.emit(visible[0].target.id);
      },
      { root: this.host.nativeElement, threshold: 0, rootMargin: '-36px 0px -60% 0px' }
    );

    this.host.nativeElement.querySelectorAll<HTMLElement>(this.sectionSelector)
      .forEach(el => this.observer!.observe(el));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
