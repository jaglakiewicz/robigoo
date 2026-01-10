import { Directive, ElementRef, AfterViewInit, OnDestroy, Input, NgZone, Renderer2 } from '@angular/core';

/**
 * Directive that adds dynamic fade shadow overlays to scrollable containers.
 * Shows gradient shadows on edges when there's more content to scroll.
 * 
 * Uses CSS pseudo-elements for shadows that overlay the content edges,
 * providing a visual indicator of scrollable content without clipping.
 * 
 * Features:
 * - Automatically updates on scroll, resize, and content changes
 * - Smooth transitions for shadow appearance
 * - Works with both horizontal and vertical scrolling
 * 
 * Usage:
 *   <div class="scrollable-list" appScrollFade>...</div>
 *   <div class="horizontal-tabs" appScrollFade [horizontal]="true">...</div>
 */
@Directive({
  selector: '[appScrollFade]'
})
export class ScrollFadeDirective implements AfterViewInit, OnDestroy {
  @Input() horizontal = false;
  @Input() fadeSize = 36; // pixels - size of fade gradient
  
  private resizeObserver: ResizeObserver | null = null;
  private mutationObserver: MutationObserver | null = null;
  private scrollListener: (() => void) | null = null;
  private wrapperElement: HTMLElement | null = null;
  private beforeShadow: HTMLElement | null = null;
  private afterShadow: HTMLElement | null = null;
  private rafId: number | null = null;
  
  constructor(
    private el: ElementRef<HTMLElement>,
    private ngZone: NgZone,
    private renderer: Renderer2
  ) {}
  
  ngAfterViewInit(): void {
    this.ngZone.runOutsideAngular(() => {
      this.setupWrapper();
      this.setupShadowElements();
      this.setupScrollListener();
      this.setupResizeObserver();
      this.setupMutationObserver();
      // Initial update with slight delay for layout
      requestAnimationFrame(() => this.updateFade());
    });
  }
  
  ngOnDestroy(): void {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
    if (this.scrollListener) {
      this.scrollListener();
    }
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
    // Remove wrapper and restore original structure
    if (this.wrapperElement && this.wrapperElement.parentNode) {
      const parent = this.wrapperElement.parentNode;
      const scrollEl = this.el.nativeElement;
      parent.insertBefore(scrollEl, this.wrapperElement);
      parent.removeChild(this.wrapperElement);
    }
  }
  
  /**
   * Wrap the scrollable element in a container that can hold shadow overlays
   */
  private setupWrapper(): void {
    const el = this.el.nativeElement;
    const parent = el.parentNode;
    
    if (!parent) return;
    
    // Create wrapper
    this.wrapperElement = this.renderer.createElement('div');
    this.renderer.addClass(this.wrapperElement, 'scroll-fade-wrapper');
    this.renderer.setStyle(this.wrapperElement, 'position', 'relative');
    this.renderer.setStyle(this.wrapperElement, 'display', 'contents');
    
    // The element itself needs position relative for shadow positioning
    this.renderer.setStyle(el, 'position', 'relative');
  }
  
  /**
   * Create the shadow overlay elements
   */
  private setupShadowElements(): void {
    const el = this.el.nativeElement;
    
    // Before shadow (top or left)
    this.beforeShadow = this.renderer.createElement('div');
    this.renderer.addClass(this.beforeShadow, 'scroll-fade-shadow');
    this.renderer.addClass(this.beforeShadow, this.horizontal ? 'scroll-fade-left' : 'scroll-fade-top');
    this.setupShadowStyles(this.beforeShadow, 'before');
    el.appendChild(this.beforeShadow);
    
    // After shadow (bottom or right)
    this.afterShadow = this.renderer.createElement('div');
    this.renderer.addClass(this.afterShadow, 'scroll-fade-shadow');
    this.renderer.addClass(this.afterShadow, this.horizontal ? 'scroll-fade-right' : 'scroll-fade-bottom');
    this.setupShadowStyles(this.afterShadow, 'after');
    el.appendChild(this.afterShadow);
  }
  
  /**
   * Apply base styles to shadow elements
   */
  private setupShadowStyles(shadow: HTMLElement, position: 'before' | 'after'): void {
    this.renderer.setStyle(shadow, 'position', 'sticky');
    this.renderer.setStyle(shadow, 'pointer-events', 'none');
    this.renderer.setStyle(shadow, 'z-index', '10');
    this.renderer.setStyle(shadow, 'flex-shrink', '0');
    this.renderer.setStyle(shadow, 'transition', 'opacity 0.15s ease');
    this.renderer.setStyle(shadow, 'opacity', '0');
    
    if (this.horizontal) {
      // Horizontal shadows
      this.renderer.setStyle(shadow, 'width', `${this.fadeSize}px`);
      this.renderer.setStyle(shadow, 'height', '100%');
      this.renderer.setStyle(shadow, 'top', '0');
      this.renderer.setStyle(shadow, 'margin-top', '0');
      
      if (position === 'before') {
        this.renderer.setStyle(shadow, 'left', '0');
        this.renderer.setStyle(shadow, 'margin-right', `-${this.fadeSize}px`);
        this.renderer.setStyle(shadow, 'background', 
          'linear-gradient(to right, var(--surface, #fff) 0%, transparent 100%)');
      } else {
        this.renderer.setStyle(shadow, 'right', '0');
        this.renderer.setStyle(shadow, 'margin-left', `-${this.fadeSize}px`);
        this.renderer.setStyle(shadow, 'background', 
          'linear-gradient(to left, var(--surface, #fff) 0%, transparent 100%)');
      }
    } else {
      // Vertical shadows
      this.renderer.setStyle(shadow, 'width', '100%');
      this.renderer.setStyle(shadow, 'height', `${this.fadeSize}px`);
      this.renderer.setStyle(shadow, 'left', '0');
      
      if (position === 'before') {
        this.renderer.setStyle(shadow, 'top', '0');
        this.renderer.setStyle(shadow, 'margin-bottom', `-${this.fadeSize}px`);
        this.renderer.setStyle(shadow, 'background', 
          'linear-gradient(to bottom, var(--surface, #fff) 0%, transparent 100%)');
      } else {
        this.renderer.setStyle(shadow, 'bottom', '0');
        this.renderer.setStyle(shadow, 'margin-top', `-${this.fadeSize}px`);
        this.renderer.setStyle(shadow, 'background', 
          'linear-gradient(to top, var(--surface, #fff) 0%, transparent 100%)');
      }
    }
  }
  
  private setupScrollListener(): void {
    const element = this.el.nativeElement;
    const handler = () => this.scheduleUpdate();
    element.addEventListener('scroll', handler, { passive: true });
    this.scrollListener = () => element.removeEventListener('scroll', handler);
  }
  
  private setupResizeObserver(): void {
    this.resizeObserver = new ResizeObserver(() => this.scheduleUpdate());
    this.resizeObserver.observe(this.el.nativeElement);
    
    // Also observe children for size changes
    const children = this.el.nativeElement.children;
    for (let i = 0; i < children.length; i++) {
      const child = children[i];
      if (child !== this.beforeShadow && child !== this.afterShadow) {
        this.resizeObserver.observe(child);
      }
    }
  }
  
  private setupMutationObserver(): void {
    this.mutationObserver = new MutationObserver(() => {
      this.scheduleUpdate();
    });
    
    this.mutationObserver.observe(this.el.nativeElement, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ['class', 'style']
    });
  }
  
  /**
   * Schedule update on next animation frame to batch multiple changes
   */
  private scheduleUpdate(): void {
    if (this.rafId !== null) return;
    
    this.rafId = requestAnimationFrame(() => {
      this.rafId = null;
      this.updateFade();
    });
  }
  
  private updateFade(): void {
    if (!this.beforeShadow || !this.afterShadow) return;
    
    const el = this.el.nativeElement;
    
    if (this.horizontal) {
      this.updateHorizontalFade(el);
    } else {
      this.updateVerticalFade(el);
    }
  }
  
  private updateVerticalFade(el: HTMLElement): void {
    const { scrollTop, scrollHeight, clientHeight } = el;
    const maxScroll = scrollHeight - clientHeight;
    
    // No scroll needed - hide shadows
    if (maxScroll <= 1) {
      this.renderer.setStyle(this.beforeShadow, 'opacity', '0');
      this.renderer.setStyle(this.afterShadow, 'opacity', '0');
      return;
    }
    
    // Calculate opacity based on scroll position (0 to 1)
    const threshold = this.fadeSize;
    const beforeOpacity = Math.min(scrollTop / threshold, 1);
    const afterOpacity = Math.min((maxScroll - scrollTop) / threshold, 1);
    
    this.renderer.setStyle(this.beforeShadow, 'opacity', String(beforeOpacity));
    this.renderer.setStyle(this.afterShadow, 'opacity', String(afterOpacity));
  }
  
  private updateHorizontalFade(el: HTMLElement): void {
    const { scrollLeft, scrollWidth, clientWidth } = el;
    const maxScroll = scrollWidth - clientWidth;
    
    // No scroll needed - hide shadows
    if (maxScroll <= 1) {
      this.renderer.setStyle(this.beforeShadow, 'opacity', '0');
      this.renderer.setStyle(this.afterShadow, 'opacity', '0');
      return;
    }
    
    // Calculate opacity based on scroll position (0 to 1)
    const threshold = this.fadeSize;
    const beforeOpacity = Math.min(scrollLeft / threshold, 1);
    const afterOpacity = Math.min((maxScroll - scrollLeft) / threshold, 1);
    
    this.renderer.setStyle(this.beforeShadow, 'opacity', String(beforeOpacity));
    this.renderer.setStyle(this.afterShadow, 'opacity', String(afterOpacity));
  }
}
