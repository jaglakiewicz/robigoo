import { Directive, ElementRef, AfterViewInit, OnDestroy, Input, NgZone } from '@angular/core';

/**
 * Directive that auto-scrolls to show clicked items with their neighbors visible.
 * Always ensures previous and next elements are visible when an item is selected.
 * On init, scrolls to the first position.
 * 
 * Usage:
 *   <div class="scrollable-list" appScrollCenter>
 *     <div class="item" *ngFor="let item of items" (click)="select(item)">...</div>
 *   </div>
 */
@Directive({
  selector: '[appScrollCenter]'
})
export class ScrollCenterDirective implements AfterViewInit, OnDestroy {
  @Input() horizontal = false;
  @Input() activeSelector = '.active';
  @Input() itemSelector = ':scope > *';
  
  private mutationObserver: MutationObserver | null = null;
  private clickListener: ((e: Event) => void) | null = null;
  private initialized = false;
  
  constructor(
    private el: ElementRef<HTMLElement>,
    private ngZone: NgZone
  ) {}
  
  ngAfterViewInit(): void {
    this.ngZone.runOutsideAngular(() => {
      this.setupClickListener();
      this.setupMutationObserver();
      // Initialize: scroll to top/start on first load
      this.scrollToStart();
    });
  }
  
  ngOnDestroy(): void {
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
    if (this.clickListener) {
      this.el.nativeElement.removeEventListener('click', this.clickListener);
    }
  }
  
  /**
   * Scroll to the start position (first element)
   */
  private scrollToStart(): void {
    const container = this.el.nativeElement;
    // Immediate scroll to start (no animation on init)
    if (this.horizontal) {
      container.scrollLeft = 0;
    } else {
      container.scrollTop = 0;
    }
    this.initialized = true;
  }
  
  private setupClickListener(): void {
    this.clickListener = (e: Event) => {
      const target = e.target as HTMLElement;
      const item = this.findClickedItem(target);
      if (item) {
        // Small delay to allow Angular to update classes
        setTimeout(() => this.scrollToShowWithNeighbors(item), 50);
      }
    };
    this.el.nativeElement.addEventListener('click', this.clickListener);
  }
  
  private setupMutationObserver(): void {
    this.mutationObserver = new MutationObserver((mutations) => {
      for (const mutation of mutations) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const target = mutation.target as HTMLElement;
          if (target.matches(this.activeSelector)) {
            this.scrollToShowWithNeighbors(target);
            break;
          }
        }
      }
    });
    
    this.mutationObserver.observe(this.el.nativeElement, {
      attributes: true,
      attributeFilter: ['class'],
      subtree: true
    });
  }
  
  private findClickedItem(target: HTMLElement): HTMLElement | null {
    const container = this.el.nativeElement;
    const items = container.querySelectorAll(this.itemSelector);
    
    for (const item of Array.from(items)) {
      if (item === target || item.contains(target)) {
        return item as HTMLElement;
      }
    }
    return null;
  }
  
  /**
   * Scrolls to ensure the selected item AND its neighbors are visible.
   * Priority: always show prev (if exists) + current + next (if exists)
   */
  private scrollToShowWithNeighbors(item: HTMLElement): void {
    const container = this.el.nativeElement;
    
    if (this.horizontal) {
      this.scrollHorizontalWithNeighbors(container, item);
    } else {
      this.scrollVerticalWithNeighbors(container, item);
    }
  }
  
  private scrollVerticalWithNeighbors(container: HTMLElement, item: HTMLElement): void {
    const prevSibling = item.previousElementSibling as HTMLElement | null;
    const nextSibling = item.nextElementSibling as HTMLElement | null;
    
    const containerHeight = container.clientHeight;
    const currentScrollTop = container.scrollTop;
    
    // Calculate the region we want to show (prev + current + next)
    const regionTop = prevSibling ? prevSibling.offsetTop : item.offsetTop;
    const regionBottom = nextSibling 
      ? nextSibling.offsetTop + nextSibling.offsetHeight 
      : item.offsetTop + item.offsetHeight;
    
    const regionHeight = regionBottom - regionTop;
    
    // Current visible area
    const visibleTop = currentScrollTop;
    const visibleBottom = currentScrollTop + containerHeight;
    
    let targetScrollTop = currentScrollTop;
    
    // Check if region fits in container
    if (regionHeight <= containerHeight) {
      // Region fits - center it in the viewport
      const regionCenter = regionTop + (regionHeight / 2);
      targetScrollTop = regionCenter - (containerHeight / 2);
    } else {
      // Region doesn't fit - prioritize showing prev at top if going up,
      // or next at bottom if going down
      // Simple approach: ensure prev is at top if it exists, otherwise current at top
      if (prevSibling) {
        // Make sure prev is visible at top
        if (prevSibling.offsetTop < visibleTop) {
          targetScrollTop = prevSibling.offsetTop;
        } else if (nextSibling && nextSibling.offsetTop + nextSibling.offsetHeight > visibleBottom) {
          // Next is cut off - scroll to show it, but keep prev if possible
          targetScrollTop = nextSibling.offsetTop + nextSibling.offsetHeight - containerHeight;
          // But don't hide prev
          if (targetScrollTop > prevSibling.offsetTop) {
            targetScrollTop = prevSibling.offsetTop;
          }
        }
      } else {
        // No prev - show current at top with next below
        targetScrollTop = item.offsetTop;
      }
    }
    
    // Clamp to valid scroll range
    const maxScroll = container.scrollHeight - containerHeight;
    targetScrollTop = Math.max(0, Math.min(targetScrollTop, maxScroll));
    
    container.scrollTo({
      top: targetScrollTop,
      behavior: 'smooth'
    });
  }
  
  private scrollHorizontalWithNeighbors(container: HTMLElement, item: HTMLElement): void {
    const prevSibling = item.previousElementSibling as HTMLElement | null;
    const nextSibling = item.nextElementSibling as HTMLElement | null;
    
    const containerWidth = container.clientWidth;
    const currentScrollLeft = container.scrollLeft;
    
    // Calculate the region we want to show (prev + current + next)
    const regionLeft = prevSibling ? prevSibling.offsetLeft : item.offsetLeft;
    const regionRight = nextSibling 
      ? nextSibling.offsetLeft + nextSibling.offsetWidth 
      : item.offsetLeft + item.offsetWidth;
    
    const regionWidth = regionRight - regionLeft;
    
    // Current visible area
    const visibleLeft = currentScrollLeft;
    const visibleRight = currentScrollLeft + containerWidth;
    
    let targetScrollLeft = currentScrollLeft;
    
    // Check if region fits in container
    if (regionWidth <= containerWidth) {
      // Region fits - center it in the viewport
      const regionCenter = regionLeft + (regionWidth / 2);
      targetScrollLeft = regionCenter - (containerWidth / 2);
    } else {
      // Region doesn't fit
      if (prevSibling) {
        if (prevSibling.offsetLeft < visibleLeft) {
          targetScrollLeft = prevSibling.offsetLeft;
        } else if (nextSibling && nextSibling.offsetLeft + nextSibling.offsetWidth > visibleRight) {
          targetScrollLeft = nextSibling.offsetLeft + nextSibling.offsetWidth - containerWidth;
          if (targetScrollLeft > prevSibling.offsetLeft) {
            targetScrollLeft = prevSibling.offsetLeft;
          }
        }
      } else {
        targetScrollLeft = item.offsetLeft;
      }
    }
    
    // Clamp to valid scroll range
    const maxScroll = container.scrollWidth - containerWidth;
    targetScrollLeft = Math.max(0, Math.min(targetScrollLeft, maxScroll));
    
    container.scrollTo({
      left: targetScrollLeft,
      behavior: 'smooth'
    });
  }
  
  private getGap(container: HTMLElement): number {
    const style = getComputedStyle(container);
    const gap = parseFloat(style.gap) || 0;
    return gap;
  }
  
  /**
   * Public method to manually scroll to active item
   */
  scrollToActive(): void {
    const activeItem = this.el.nativeElement.querySelector(this.activeSelector) as HTMLElement;
    if (activeItem) {
      this.scrollToShowWithNeighbors(activeItem);
    }
  }
}
