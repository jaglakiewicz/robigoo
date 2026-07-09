import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { UserService, CreateUserRequest, UserDTO } from '../services/user.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { Step } from '../shared/components/step-indicator/step-indicator.component';
import { ScrollSpyDirective } from '../shared/directives/scroll-spy.directive';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  @ViewChild(ScrollSpyDirective) private scrollSpy!: ScrollSpyDirective;
  activeSection = 0;
  activeSubKey = '';
  currentStep = 0; // kept for compatibility
  private editUserListener: any;
  SVG_ICONS = SVG_ICONS; // Make SVG_ICONS available in template
  private focusMode = false; // Track if we're in focus mode
  private scrollTimeout: any;

  get saving(): boolean {
    return this.savingAppSettings || this.savingSettings;
  }

  steps: Step[] = [
    {
      id: 0, key: 'program', label: 'Ustawienia programu',
      children: [
        { id: 0, key: 'sub-org',          label: 'Jednostka' },
        { id: 0, key: 'sub-docs',         label: 'Dokumenty' },
        { id: 0, key: 'sub-protocol',     label: 'Protokół' },
        { id: 0, key: 'sub-register',     label: 'Rejestr' },
        { id: 0, key: 'sub-controlmarks', label: 'Znaki kontrolne' },
      ]
    },
    { id: 1, key: 'user',  label: 'Ustawienia użytkownika' },
    { id: 2, key: 'admin', label: 'Zarządzanie użytkownikami' }
  ];

  // -- App Settings ------------------------------------------------------
  appSettings = {
    organization: {
      stationName: '', unitAuthorizationNumber: '',
      addressLine1: '', addressLine2: '', city: '', postalCode: '', postOffice: '',
      phone: '', email: '', taxId: '', regon: ''
    },
    documents: {
      protocol: {
        inspectionValidityYears: 3, numberPrefix: '', sequencePadding: 3,
        numberFormat: '{PREFIX}/{YEAR}/{SEQ}', header: '', footer: ''
      },
      register: { header: '', footer: '' },
      controlMarks: { header: '', footer: '' }
    }
  };

  savingAppSettings = false;

  readonly formatTokens = [
    { token: '{PREFIX}', hint: 'Prefiks (np. SKO)' },
    { token: '{YEAR}',   hint: 'Rok (np. 2025)' },
    { token: '{MONTH}',  hint: 'Miesiac (np. 06)' },
    { token: '{SEQ}',    hint: 'Numer sekwencyjny (np. 001)' },
    { token: '{SEQ4}',   hint: 'Numer 4-cyfrowy (np. 0001)' },
  ];

  get protocolNumberPreview(): string {
    const now = new Date();
    const p = this.appSettings.documents.protocol;
    return (p.numberFormat || '{YEAR}/{SEQ}')
      .replace('{PREFIX}', p.numberPrefix || '')
      .replace('{YEAR}',   String(now.getFullYear()))
      .replace('{MONTH}',  String(now.getMonth() + 1).padStart(2, '0'))
      .replace('{SEQ}',    '1'.padStart(p.sequencePadding || 3, '0'))
      .replace('{SEQ4}',   '1'.padStart(4, '0'));
  }

  insertToken(token: string): void {
    this.appSettings.documents.protocol.numberFormat =
      (this.appSettings.documents.protocol.numberFormat || '') + token;
  }

  // User Settings
  userSettings = {
    login: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    theme: 'light',
    avatar: null as string | null
  };

  // Password change
  passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  changePasswordError = '';
  changePasswordSuccess = false;
  changingPassword = false;
  showPasswordForm = false;

  // Settings save messages
  userSettingsError = '';
  userSettingsSuccess = false;
  savingSettings = false;
  avatarError = '';
  avatarSuccess = false;
  signatureImage: string | null = null;
  signatureError = '';
  signatureSuccess = false;

  // Admin Settings
  adminSettings = {
    enableLogs: true,
    logLevel: 'info',
    maxUsers: 100
  };

  // User Management
  users: UserDTO[] = [];
  showCreateUserForm = false;
  newUserForm = {
    login: '',
    password: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    role: 'user'
  };
  creatingUser = false;

  // Password confirmation dialog
  showPasswordConfirmDialog = false;
  confirmDialogAction: 'delete' | 'create' = 'delete';
  userToConfirm: UserDTO | null = null;
  confirmDialogPassword = '';
  confirmDialogError = '';
  confirmingAction = false;
  deletingUserId: number | null = null;

  // Role change
  updatingRoleUserId: number | null = null;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer,
    private el: ElementRef,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    try {
      console.log('[Settings] Component initialized');
      
      // Listener for edit user settings event
      this.editUserListener = (event: any) => {
        this.scrollToSection(1);
      };
      window.addEventListener('editUserEvent', this.editUserListener);

      this.loadUserSettings();
      this.loadUsers();
      this.loadAppSettings();

      // Listen for scroll events to clear focus mode
      const container = this.el.nativeElement.querySelector('.settings-scroll');
      if (container) {
        container.addEventListener('scroll', this.onManualScroll.bind(this));
      }
    } catch (error) {
      console.error('[Settings] Error during initialization:', error);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('editUserEvent', this.editUserListener);
    const container = this.el.nativeElement.querySelector('.settings-scroll');
    if (container) {
      container.removeEventListener('scroll', this.onManualScroll.bind(this));
    }
    if (this.scrollTimeout) {
      clearTimeout(this.scrollTimeout);
    }
  }

  private onManualScroll(): void {
    // Clear any pending timeout
    if (this.scrollTimeout) {
      clearTimeout(this.scrollTimeout);
    }
    
    // Set a timeout to clear focus mode after user stops scrolling
    this.scrollTimeout = setTimeout(() => {
      if (this.focusMode) {
        this.clearFocusMode();
      }
    }, 150);
  }

  private applyFocusMode(targetSectionId: number): void {
    this.focusMode = true;
    const sections = this.el.nativeElement.querySelectorAll('.settings-section');
    sections.forEach((section: HTMLElement) => {
      const sectionId = section.id.replace('section-', '');
      if (parseInt(sectionId) === targetSectionId) {
        section.classList.remove('blurred');
      } else {
        section.classList.add('blurred');
      }
    });
  }

  private clearFocusMode(): void {
    this.focusMode = false;
    const sections = this.el.nativeElement.querySelectorAll('.settings-section');
    sections.forEach((section: HTMLElement) => {
      section.classList.remove('blurred');
    });
  }

  loadUserSettings(): void {
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.userSettings.login = currentUser.login;
      this.userSettings.firstName = currentUser.firstName;
      this.userSettings.lastName = currentUser.lastName;
      this.userSettings.email = currentUser.email;
      this.userSettings.phone = currentUser.phone;
      this.userSettings.permissionNumber = currentUser.permissionNumber;
      this.userSettings.theme = (currentUser as any).theme || 'light';
      if (currentUser.avatarBase64) {
        this.userSettings.avatar = 'data:image/png;base64,' + currentUser.avatarBase64;
      }
      if ((currentUser as any).signatureBase64) {
        this.signatureImage = 'data:image/png;base64,' + (currentUser as any).signatureBase64;
      }
    }
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe(
      (data: any) => {
        if (data && Array.isArray(data)) {
          this.users = data;
        } else {
          console.error('[Settings] Unexpected response format:', data);
        }
      },
      (error: any) => {
        console.error('[Settings] Error loading users:', error);
      }
    );
  }

  saveUserSettings(): void {
    this.saveUserSettingsInternal();
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      // Clear form when closing
      this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      this.changePasswordError = '';
    }
  }

  changePassword(): void {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.notificationService.error('Hasla nie pasuja do siebie');
      return;
    }

    if (this.passwordForm.newPassword.length < 6) {
      this.notificationService.error('Hasło musi mieć co najmniej 6 znaków');
      return;
    }

    this.changingPassword = true;

    this.userService.changePassword({
      oldPassword: this.passwordForm.oldPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe(
      (response) => {
        console.log('[Settings] Password changed:', response);
        this.notificationService.success('Haslo zostalo zmienione pomyslnie!');
        this.changingPassword = false;
        this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
        this.showPasswordForm = false;
        this.passwordDrawerOpen = false;
      },
      (error: any) => {
        console.error('[Settings] Error changing password:', error);
        const errorMsg = error.error?.message || 'Blad podczas zmiany hasla';
        this.notificationService.error(errorMsg);
        this.changingPassword = false;
      }
    );
  }

  onAvatarSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      // Validate file size (max 2MB)
      if (file.size > 2097152) { // 2MB
        this.avatarError = 'Plik jest za duzy (maksymalnie 2MB)';
        return;
      }

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.avatarError = 'Plik musi byc obrazem (JPG, PNG, GIF)';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.userService.uploadAvatar(file).subscribe(
          (response) => {
            console.log('[Settings] Avatar uploaded:', response);
            // Reload current user to get updated avatar
            this.loadUserSettings();
            this.avatarSuccess = true;
            this.avatarError = '';
            setTimeout(() => { this.avatarSuccess = false; }, 3000);
          },
          (error: any) => {
            console.error('[Settings] Error uploading avatar:', error);
            this.avatarError = error.error?.message || 'Blad podczas przesylania avatara';
          }
        );
      };
      reader.readAsDataURL(file);
    }
  }

  onSignatureSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;
    if (file.size > 2097152) { this.signatureError = 'Plik jest za duzy (max 2MB)'; return; }
    if (!file.type.startsWith('image/')) { this.signatureError = 'Plik musi byc obrazem'; return; }
    this.userService.uploadSignature(file).subscribe({
      next: (res) => {
        this.signatureImage = 'data:image/png;base64,' + res.signatureBase64;
        this.signatureSuccess = true;
        this.signatureError = '';
        setTimeout(() => { this.signatureSuccess = false; }, 3000);
      },
      error: (err) => { this.signatureError = err.error?.message || 'Blad podczas przeslania podpisu'; }
    });
  }

  passwordDrawerOpen = false;
  createUserDrawerOpen = false;

  openPasswordDrawer(): void {
    this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
    this.passwordDrawerOpen = true;
  }
  closePasswordDrawer(): void { this.passwordDrawerOpen = false; }

  openCreateUserDrawer(): void {
    this.resetCreateUserForm();
    this.createUserDrawerOpen = true;
  }
  closeCreateUserDrawer(): void { this.createUserDrawerOpen = false; }

  createUserFromDrawer(): void {
    this.createUser();
    if (!this.creatingUser) this.createUserDrawerOpen = false;
  }

  canSubmitPassword(): boolean {
    return !!this.passwordForm.oldPassword &&
           !!this.passwordForm.newPassword &&
           this.passwordForm.newPassword === this.passwordForm.confirmPassword &&
           this.passwordForm.newPassword.length >= 6;
  }

  getPasswordStrengthClass(): string {
    const p = this.passwordForm.newPassword;
    if (!p) return '';
    let score = 0;
    if (p.length >= 8) score++;
    if (p.length >= 12) score++;
    if (/[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p)) score++;
    if (/[^A-Za-z0-9]/.test(p)) score++;
    if (score <= 1) return 'strength-weak';
    if (score <= 3) return 'strength-medium';
    return 'strength-strong';
  }

  getPasswordStrengthLabel(): string {
    const c = this.getPasswordStrengthClass();
    if (c === 'strength-weak') return 'Slabe';
    if (c === 'strength-medium') return 'Srednie';
    return 'Silne';
  }

  scrollToSection(sectionId: number): void {
    console.log('[Settings] Scrolling to section:', sectionId);
    
    const container = this.el.nativeElement.querySelector('.settings-scroll') as HTMLElement;
    const section = this.el.nativeElement.querySelector(`#section-${sectionId}`) as HTMLElement;
    
    if (!container || !section) {
      console.error('[Settings] Container or section not found');
      return;
    }

    // Update active section
    this.activeSection = sectionId;
    const step = this.steps.find(s => s.id === sectionId);
    this.activeSubKey = step?.children?.[0]?.key ?? '';
    
    // Apply focus mode
    this.applyFocusMode(sectionId);
    
    // Lock scroll spy temporarily
    if (this.scrollSpy) {
      this.scrollSpy.lock();
    }
    
    // Calculate scroll position
    // We want the section to appear at the top of the container
    const containerTop = container.getBoundingClientRect().top;
    const sectionTop = section.getBoundingClientRect().top;
    const currentScroll = container.scrollTop;
    const targetScroll = currentScroll + (sectionTop - containerTop) - 20; // 20px padding
    
    console.log('[Settings] Scroll calculation:', {
      containerTop,
      sectionTop,
      currentScroll,
      targetScroll
    });
    
    // Perform smooth scroll
    container.scrollTo({
      top: targetScroll,
      behavior: 'smooth'
    });
  }

  scrollToSubSection(key: string): void {
    console.log('[Settings] Scrolling to subsection:', key);
    
    const container = this.el.nativeElement.querySelector('.settings-scroll') as HTMLElement;
    const element = this.el.nativeElement.querySelector(`#${key}`) as HTMLElement;
    
    if (!container || !element) {
      console.error('[Settings] Container or element not found');
      return;
    }

    // Update active subsection
    this.activeSubKey = key;
    
    // Find parent section
    const parentSection = element.closest('.settings-section') as HTMLElement;
    if (parentSection) {
      const sectionId = parseInt(parentSection.id.replace('section-', ''));
      this.activeSection = sectionId;
      this.applyFocusMode(sectionId);
    }
    
    // Lock scroll spy temporarily
    if (this.scrollSpy) {
      this.scrollSpy.lock();
    }
    
    // Calculate scroll position
    const containerTop = container.getBoundingClientRect().top;
    const elementTop = element.getBoundingClientRect().top;
    const currentScroll = container.scrollTop;
    const targetScroll = currentScroll + (elementTop - containerTop) - 20; // 20px padding
    
    console.log('[Settings] Scroll calculation:', {
      containerTop,
      elementTop,
      currentScroll,
      targetScroll
    });
    
    // Perform smooth scroll
    container.scrollTo({
      top: targetScroll,
      behavior: 'smooth'
    });
  }

  onActiveSection(id: string): void {
    const num = parseInt(id.replace('section-', ''), 10);
    if (!isNaN(num) && num !== this.activeSection) {
      this.activeSection = num;
      // Reset sub-key to first child of the newly active section
      const step = this.steps.find(s => s.id === num);
      this.activeSubKey = step?.children?.[0]?.key ?? '';
    }
  }

  goToStep(stepId: number): void {
    this.scrollToSection(stepId);
  }

  loadAppSettings(): void {
    this.http.get<any>('/api/settings').subscribe({
      next: (data) => { this.appSettings = { ...this.appSettings, ...data }; },
      error: () => {}
    });
  }

  saveAppSettings(): void {
    this.savingAppSettings = true;
    this.http.put('/api/settings', this.appSettings).subscribe({
      next: () => { this.notificationService.success('Ustawienia programu zapisane'); this.savingAppSettings = false; },
      error: (err) => { this.notificationService.error(err.error?.message || 'Blad zapisu'); this.savingAppSettings = false; }
    });
  }

  saveAll(): void {
    // Save both app settings and user settings, show single success message
    let appSettingsSaved = false;
    let userSettingsSaved = false;
    let hasError = false;

    // Save app settings (admin only)
    if (this.isCurrentUserAdmin()) {
      this.savingAppSettings = true;
      this.http.put('/api/settings', this.appSettings).subscribe({
        next: () => {
          this.savingAppSettings = false;
          appSettingsSaved = true;
          if (userSettingsSaved && !hasError) {
            this.notificationService.success('Wszystkie ustawienia zostaly zapisane');
          }
        },
        error: (err) => {
          this.notificationService.error(err.error?.message || 'Blad zapisu ustawien programu');
          this.savingAppSettings = false;
          hasError = true;
        }
      });
    } else {
      appSettingsSaved = true; // Skip for non-admin
    }

    // Save user settings
    this.saveUserSettingsInternal((success) => {
      userSettingsSaved = success;
      if (appSettingsSaved && success && !hasError) {
        this.notificationService.success('Wszystkie ustawienia zostaly zapisane');
      }
    });
  }

  private saveUserSettingsInternal(callback?: (success: boolean) => void): void {
    this.userSettingsError = '';
    this.userSettingsSuccess = false;
    this.savingSettings = true;

    const currentUser = this.authService.getCurrentUser();
    const isAdmin = currentUser && currentUser.role === 'admin';

    // Validate required fields
    if (!this.userSettings.firstName || !this.userSettings.lastName) {
      this.notificationService.error('Imie i nazwisko sa wymagane');
      this.savingSettings = false;
      if (callback) callback(false);
      return;
    }

    if (!this.userSettings.email) {
      this.notificationService.error('Email jest wymagany');
      this.savingSettings = false;
      if (callback) callback(false);
      return;
    }

    // Build update data with all required fields
    // Server requires: firstName, lastName, email, language, theme
    // Phone and permissionNumber are optional
    const updateData: any = {
      firstName: this.userSettings.firstName.trim(),
      lastName: this.userSettings.lastName.trim(),
      email: this.userSettings.email.trim(),
      language: 'pl',  // Always Polish
      theme: this.userSettings.theme || 'light'
    };

    // Only include phone if it has a value
    if (this.userSettings.phone && this.userSettings.phone.trim()) {
      updateData.phone = this.userSettings.phone.trim();
    }

    // Only include permissionNumber if it has a value
    if (this.userSettings.permissionNumber && this.userSettings.permissionNumber.trim()) {
      updateData.permissionNumber = this.userSettings.permissionNumber.trim();
    }

    console.log('[Settings] Saving user settings with data:', JSON.stringify(updateData, null, 2));

    this.userService.updateProfile(updateData).subscribe(
      (response) => {
        console.log('[Settings] Profile updated successfully:', response);
        if (!callback) {
          // Only show message if not called from saveAll
          this.notificationService.success('Ustawienia zostaly pomyslnie zapisane!');
        }
        this.savingSettings = false;

        // Update current user in session storage
        const currentUser = this.authService.getCurrentUser();
        if (currentUser) {
          // Update all fields that were changed
          currentUser.email = this.userSettings.email;
          currentUser.phone = this.userSettings.phone;
          (currentUser as any).theme = this.userSettings.theme;
          
          // Update admin-editable fields if user is admin
          if (isAdmin) {
            currentUser.firstName = this.userSettings.firstName;
            currentUser.lastName = this.userSettings.lastName;
            currentUser.permissionNumber = this.userSettings.permissionNumber;
          }

          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
        }
        
        if (callback) callback(true);
      },
      (error: any) => {
        console.error('[Settings] Error saving settings:', error);
        console.error('[Settings] Error details:', JSON.stringify(error, null, 2));
        if (error.error) {
          console.error('[Settings] Server error response:', error.error);
        }
        const errorMsg = error.error?.message || 'Blad podczas zapisywania ustawien';
        this.notificationService.error(errorMsg);
        this.savingSettings = false;
        if (callback) callback(false);
      }
    );
  }

  get visibleSteps(): Step[] {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || currentUser.role !== 'admin') {
      // Non-admin users see only steps 0 and 1
      return [this.steps[0], this.steps[1]];
    }
    // Admin users see all steps
    return this.steps;
  }

  isCurrentUserAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser && currentUser.role === 'admin';
  }

  toggleCreateUserForm(): void {
    this.showCreateUserForm = !this.showCreateUserForm;
    if (!this.showCreateUserForm) {
      this.resetCreateUserForm();
    }
  }

  resetCreateUserForm(): void {
    const currentUser = this.authService.getCurrentUser();
    
    this.newUserForm = {
      login: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      permissionNumber: '',
      role: (currentUser && currentUser.login !== 'admin') ? 'user' : 'user'
    };
  }

  canSelectRoleWhenCreating(): boolean {
    const currentUser = this.authService.getCurrentUser();
    // Only master admin can select role when creating users
    return currentUser && currentUser.login === 'admin';
  }

  createUser(): void {
    if (!this.newUserForm.login || !this.newUserForm.password || !this.newUserForm.firstName) {
      this.notificationService.error('Login, haslo i imie sa wymagane');
      return;
    }

    this.creatingUser = true;

    const newUser: CreateUserRequest = {
      login: this.newUserForm.login,
      password: this.newUserForm.password,
      firstName: this.newUserForm.firstName,
      lastName: this.newUserForm.lastName,
      email: this.newUserForm.email,
      phone: this.newUserForm.phone,
      permissionNumber: this.newUserForm.permissionNumber,
      role: this.newUserForm.role
    };

    this.userService.createUser(newUser).subscribe(
      (response) => {
        console.log('[Settings] User created:', response);
        this.notificationService.success('Uzytkownik zostal pomyslnie utworzony!');
        this.creatingUser = false;
        this.resetCreateUserForm();
        this.showCreateUserForm = false;
        this.createUserDrawerOpen = false;

        // Reload users list
        this.loadUsers();
      },
      (error: any) => {
        console.error('[Settings] Error creating user:', error);
        const errorMsg = error.error?.error || error.error?.message || 'Blad podczas tworzenia uzytkownika';
        this.notificationService.error(errorMsg);
        this.creatingUser = false;
      }
    );
  }

  editUser(user: UserDTO): void {
    window.dispatchEvent(new CustomEvent('editUserEvent', { detail: user }));
  }

  canDeleteUser(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    const userId = user.id || user.userId;
    const currentUserId = currentUser.userId;

    // Cannot delete self
    if (currentUserId === userId) return false;

    // Only admins can delete users
    if (currentUser.role !== 'admin') return false;

    // Master admin (login = admin) can delete anyone
    if (currentUser.login === 'admin') return true;

    // Regular admin can only delete regular users (not admins)
    return user.role !== 'admin';
  }

  openDeleteConfirmDialog(user: UserDTO): void {
    console.log('[Settings] Opening delete dialog for user:', user);
    // Ask for password confirmation before deleting
    this.userToConfirm = user;
    this.confirmDialogAction = 'delete';
    this.showPasswordConfirmDialog = true;
  }

  confirmPasswordAction(): void {
    if (!this.confirmDialogPassword || !this.userToConfirm) {
      console.error('[Settings] Missing password or user to confirm');
      return;
    }

    // Use id or userId (backend returns 'id')
    const userId = this.userToConfirm.id || this.userToConfirm.userId;
    
    console.log('[Settings] Confirming delete for user:', this.userToConfirm);
    console.log('[Settings] User ID:', userId);

    if (!userId) {
      console.error('[Settings] User ID is undefined');
      this.notificationService.error('Blad: Brak ID uzytkownika');
      return;
    }

    this.confirmDialogError = '';
    this.confirmingAction = true;

    this.userService.deleteUser(userId, this.confirmDialogPassword).subscribe(
      (response) => {
        console.log('[Settings] User deleted:', response);
        this.showPasswordConfirmDialog = false;
        this.confirmingAction = false;
        this.confirmDialogPassword = '';
        this.userToConfirm = null;

        // Reload users list
        this.loadUsers();
        this.notificationService.success('Uzytkownik zostal pomyslnie usuniety!');
      },
      (error: any) => {
        console.error('[Settings] Error deleting user:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Blad podczas usuwania uzytkownika';
        this.notificationService.error(errorMsg);
        this.confirmingAction = false;
      }
    );
  }

  cancelPasswordConfirmDialog(): void {
    this.showPasswordConfirmDialog = false;
    this.confirmDialogPassword = '';
    this.confirmDialogError = '';
    this.userToConfirm = null;
  }

  canChangeUserRole(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    const userId = user.id || user.userId;
    const currentUserId = currentUser.userId;

    // Cannot change own role
    if (currentUserId === userId) return false;

    // Only master admin (login = admin) can change roles
    // Regular admin cannot change roles of existing users
    if (currentUser.login !== 'admin') return false;

    return true;
  }

  onRoleChanged(user: UserDTO): void {
    const currentUser = this.authService.getCurrentUser();
    
    // Validation - only master admin can change roles
    if (!currentUser || currentUser.login !== 'admin') {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Tylko master administrator może zmieniać role użytkowników');
      return;
    }

    // Validation - check if user can make this change
    if (!this.canChangeUserRole(user)) {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Brak uprawnien do zmiany tej roli');
      return;
    }

    const userId = user.id || user.userId;
    if (!userId) {
      this.notificationService.error('Blad: Brak ID uzytkownika');
      return;
    }

    this.updatingRoleUserId = userId;

    this.userService.updateUserRole(userId, user.role).subscribe(
      (response) => {
        console.log('[Settings] User role updated:', response);
        this.notificationService.success(`Rola uzytkownika ${user.login} zostala zmieniona`);
        this.updatingRoleUserId = null;
      },
      (error: any) => {
        console.error('[Settings] Error updating user role:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Blad podczas zmiany roli uzytkownika';
        this.notificationService.error(errorMsg);
        this.updatingRoleUserId = null;
        // Reload users to revert the role change
        this.loadUsers();
      }
    );
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
