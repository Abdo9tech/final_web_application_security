// Role Modal Functions
function openRoleModal(userId, userName, currentRole) {
    document.getElementById('userId').value = userId;
    document.getElementById('userName').textContent = userName;
    document.getElementById('roleSelect').value = currentRole;
    document.getElementById('roleModal').classList.remove('hidden');
}

function closeRoleModal() {
    document.getElementById('roleModal').classList.add('hidden');
}

// Password Modal Functions
function openPasswordModal(userId, userName) {
    document.getElementById('passwordUserId').value = userId;
    document.getElementById('passwordUserName').textContent = userName;
    document.getElementById('newPassword').value = '';
    document.getElementById('confirmPassword').value = '';
    document.getElementById('passwordError').classList.add('hidden');
    document.getElementById('passwordModal').classList.remove('hidden');
}

function closePasswordModal() {
    document.getElementById('passwordModal').classList.add('hidden');
}

function validatePasswordComplexity(password) {
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /\d/.test(password);
    // Special characters: ! @ $ % ^ & * ( ) - = [ ] { } ; : ' " , . < > / \ |
    let hasSpecialChar = false;
    const specialChars = "!@$%^&*()-=[]{};\':\",./<>\\|";
    for (let char of password) {
        if (specialChars.includes(char)) {
            hasSpecialChar = true;
            break;
        }
    }
    return hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;
}

// Validate passwords before submission
document.querySelector('#passwordModal form')?.addEventListener('submit', function(e) {
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const errorDiv = document.getElementById('passwordError');
    const errorMessage = document.getElementById('errorMessage');

    // Reset error
    errorDiv.classList.add('hidden');

    // Validate passwords match
    if (newPassword !== confirmPassword) {
        e.preventDefault();
        errorMessage.textContent = 'Passwords do not match.';
        errorDiv.classList.remove('hidden');
        return false;
    }

    // Validate password strength (at least 8 characters)
    if (newPassword.length < 8) {
        e.preventDefault();
        errorMessage.textContent = 'Password must be at least 8 characters long.';
        errorDiv.classList.remove('hidden');
        return false;
    }

    // Validate password complexity
    if (!validatePasswordComplexity(newPassword)) {
        e.preventDefault();
        errorMessage.textContent = 'Password must contain uppercase, lowercase, number, and special character.';
        errorDiv.classList.remove('hidden');
        return false;
    }
});

// Close modals on outside click
document.getElementById('roleModal')?.addEventListener('click', function(e) {
    if (e.target === this) {
        closeRoleModal();
    }
});

document.getElementById('passwordModal')?.addEventListener('click', function(e) {
    if (e.target === this) {
        closePasswordModal();
    }
});
