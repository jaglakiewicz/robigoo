import { Directive, ElementRef, EventEmitter, Input, NgZone, OnDestroy, OnInit, Output } from '@angular/core';

@Directive({ selector: '[appScrollSpy]' })
export class ScrollSpyDirective implements OnInit, OnDestroy {
  @Input() sectionSelector = '.settings-section';
  @Output() activeSection = new EventEmitter<string>();

  private lastEmitted = '';
  private locked = false;
  private lockTimer: any;
  private onScroll = () => { if (!this.locked) this.zone.run(() => this.update()); };

  constructor(private host: ElementRef<HTMLElement>, private zone: NgZone) {}

  /** Call before any programmatic scroll to prevent the spy from overriding it. */
  lock(ms = 600): void {
    this.locked = true;
    clearTimeout(this.lockTimer);
    this.lockTimer = setTimeout(() => { this.locked = false; }, ms);
  }

  ngOnInit(): void {
    this.host.nativeElement.addEventListener('scroll', this.onScroll, { passive: true });
    setTimeout(() => this.update(), 50);
  }

  ngOnDestroy(): void {
    this.host.nativeElement.removeEventListener('scroll', this.onScroll);
    clearTimeout(this.lockTimer);
  }

  private update(): void {
    const container = this.host.nativeElement;
    const scrollTop = container.scrollTop;
    const scrollHeight = container.scrollHeight;
    const clientHeight = container.clientHeight;

    const els = Array.from(
      container.querySelectorAll<HTMLElement>(this.sectionSelector)
    ).filter(el => !!el.id);

    if (!els.length) return;

    const atBottom = scrollTop + clientHeight >= scrollHeight - 8;

    let active = els[0];
    if (atBottom) {
      for (let i = els.length - 1; i >= 0; i--) {
        if (els[i].offsetTop < scrollTop + clientHeight) { active = els[i]; break; }
      }
    } else {
      for (const el of els) {
        if (el.offsetTop - scrollTop <= 60) active = el;
        else break;
      }
    }

    if (active.id !== this.lastEmitted) {
      this.lastEmitted = active.id;
      this.activeSection.emit(active.id);
    }
  }
}
